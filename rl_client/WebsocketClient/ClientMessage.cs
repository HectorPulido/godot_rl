using System;

namespace rl_client
{
    public static class EventType
    {
        public const string
            configure = "configure",
            getInput = "get_input",
            setReward = "set_reward";
    }


    public class Message
    {
        public string eventType { get; set; }
    }

    public class ConfigureMessage : Message {
        public int numberActions;
        public float[] inputVariables;
    }

    public class GetInputMessage : Message {
        public float[] envData;
    }

    public class SetRewardMessage : Message {
        public float reward;
        public bool done;
    }

    public class Response {
        public string status;
    }

    public class ResponseInput : Response {
        public int action;
    }
}