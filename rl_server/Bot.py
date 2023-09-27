import warnings
import numpy as np
from sklearn.neural_network import MLPClassifier
from sklearn.ensemble import RandomForestClassifier
from sklearn.exceptions import ConvergenceWarning

warnings.filterwarnings("ignore", category=ConvergenceWarning)


class Bot:
    def __init__(
        self,
        number_of_actions,
        sample_size,
        percentile,
        last_threshold,
        input_variables,
        sticky_action_value=3,
    ):
        self.number_of_actions = number_of_actions
        self.sample_size = sample_size
        self.percentile = percentile
        self.last_threshold = last_threshold
        self.input_variables = input_variables
        self.sticky_action_value = sticky_action_value

        self._sticky_action_value = self.sticky_action_value

        self.agent = MLPClassifier(
            warm_start=True,
            hidden_layer_sizes=(100, 100),
            activation="relu",
            max_iter=1,
            verbose=True,
        )

        self.sessions = []
        self.batch = 0
        self.iterations = 0
        self.current_session_data = {"env_data": [], "input": [], "reward": 0}
        self.batch_states = np.array([])
        self.batch_actions = np.array([])
        self.batch_rewards = np.array([])

        self.last_action = None

        self._number_of_actions = list(range(self.number_of_actions))

        self.agent.fit(
            [self.input_variables] * self.number_of_actions, self._number_of_actions
        )

    def _clamp_min(self, n, min):
        if n < min:
            return min
        else:
            return n

    def _sticky_action(self, action):
        if self.last_action is None:
            return action
        if self._sticky_action_value > np.random.rand():
            if 0.5 > np.random.rand():
                return np.random.choice(self._number_of_actions)
            return self.last_action
        return action

    def _add_session(self):
        self.batch_states = np.append(
            self.batch_states, self.current_session_data["env_data"].copy()
        )
        self.batch_actions = np.append(
            self.batch_actions, self.current_session_data["input"].copy()
        )
        self.batch_rewards = np.append(
            self.batch_rewards, self.current_session_data["reward"]
        )
        self.sessions.append(self.current_session_data.copy())

        self._save_dataset("sessions.npy")

    def _mask_session(self, session_states, session_actions, session_rewards, mask):
        states, actions, rewards = [], [], []

        for states_batch, actions_batch, reward, valid_threshold in zip(
            session_states, session_actions, session_rewards, mask
        ):
            if valid_threshold:
                for state, action in zip(states_batch, actions_batch):
                    states.append(state)
                    actions.append(action)
                    rewards.append(reward)

        return states, actions, rewards

    def get_input(self, env_data):
        self.current_session_data["env_data"].append(env_data)
        p = self.agent.predict_proba([env_data])[0]
        action = np.random.choice(self._number_of_actions, p=p)
        action = self._sticky_action(action)
        self.last_action = action

        self.current_session_data["input"].append(action)
        return action

    def set_reward(self, reward, done):
        self.current_session_data["reward"] += reward

        if not done:
            return

        self.iterations += 1
        self._add_session()
        self.current_session_data = {"env_data": [], "input": [], "reward": 0}

        if self.iterations > self.sample_size * (self.batch + 1):
            self.last_action = None
            self.batch += 1
            self._sticky_action_value = np.exp(
                -(1 / self.sticky_action_value) * self.batch
            )

            self.train()

    def augment_dataset(self, states, actions, rewards, noise_factor):
        states = np.asarray(states)
        actions = np.asarray(actions)
        rewards = np.asarray(rewards)

        normalized_rewards = (rewards - min(rewards)) / (
            max(rewards) - min(rewards)
        )  # Normalize rewards to the range of 0 to 1

        repeat_times = np.round(normalized_rewards * 10).astype(
            int
        )  # Multiply normalized reward by 10 and round it, this will be our repeat times

        augmented_states = []
        augmented_actions = []

        for state, action, times in zip(states, actions, repeat_times):
            for _ in range(times):
                augmented_states.append(
                    state + np.random.normal(0, noise_factor, states.shape[1])
                )
                augmented_actions.append(action)

        augmented_states = np.around(np.array(augmented_states), decimals=3)
        augmented_actions = np.array(augmented_actions)
        return augmented_states, augmented_actions

    def train(self):
        threshold = self._clamp_min(
            np.percentile(self.batch_rewards, self.percentile), self.last_threshold
        )
        self.last_threshold = threshold
        threshold_condition = self.batch_rewards >= threshold

        elite_states, elite_actions, elite_rewards = self._mask_session(
            self.batch_states,
            self.batch_actions,
            self.batch_rewards,
            threshold_condition,
        )

        elite_states_concat, elite_actions_concat = self.augment_dataset(
            elite_states, elite_actions, elite_rewards, 0.01
        )

        if len(elite_states) == 0 or len(elite_actions) == 0:
            print("No hay datos suficientes para entrenar")
            return

        print(f"Training batch {self.batch}...")
        print("Sticky action value: ", self._sticky_action_value)
        print("Threshold: ", threshold)
        print("Examples: ", len(elite_rewards))

        self.agent.fit(elite_states_concat, elite_actions_concat)

    def _save_dataset(self, path):
        np.save(path, self.sessions)

    def _load_dataset(self, path):
        self.sessions = np.load(path, allow_pickle=True).tolist()
        self.batch_states = np.array(
            [session["env_data"] for session in self.sessions], dtype=object
        )
        self.batch_actions = np.array(
            [session["input"] for session in self.sessions], dtype=object
        )
        self.batch_rewards = np.array(
            [session["reward"] for session in self.sessions], dtype=object
        )

        self.agent.max_iter = 1000
        self.train()
        self.agent.max_iter = 1
