using Godot;
using System;
using rl_client;
using System.Threading.Tasks;

public partial class RLClient : Node2D
{
	[Export]
	public string ServerAddress = "ws://localhost:8080";

	public Raqueta raquetaAI;
	public Raqueta raquetaPlayer;
	public Ball ball;
	
	[Export]
	public bool scoreByCollision = true;

	private bool ready = false;
	Vector2 screenSize;
	Vector2 lastBallPosition;
	float lastRaquetaAIPosition;
	float lastRaquetaPlayerPosition;
	ClientRL clientRL;

	public override void _Ready()
	{
		screenSize = GetViewportRect().Size;
		clientRL = new ClientRL(ServerAddress);

		Task task = Task.Run(async () =>
		{
			GetTree().Paused = true;
			await clientRL.Start();

			ConfigureMessage configure = new()
			{
				eventType = EventType.configure,
				numberActions = 3,
				inputVariables = GetEnvData()
			};

			Response resp = await clientRL.SendGenericData<ConfigureMessage, Response>(configure);

			GD.Print("Client ready", resp);
			ready = true;
			GetTree().Paused = false;
		});

		raquetaAI = GetNode<Raqueta>("RaquetaAI");
		raquetaPlayer = GetNode<Raqueta>("RaquetaPlayer");
		ball = GetNode<Ball>("Ball");

		lastBallPosition = ball.Position;
		lastRaquetaAIPosition = raquetaAI.Position.Y;
		lastRaquetaPlayerPosition = raquetaPlayer.Position.Y;
	}

	private float[] GetEnvData()
	{
		var sendData = new float[] {
            NormalizeValue(ball.Position.X, screenSize.X),
            NormalizeValue(ball.Position.Y, screenSize.Y),
            NormalizeValue(lastBallPosition.X, screenSize.X),
            NormalizeValue(lastBallPosition.Y, screenSize.Y),
            NormalizeValue(raquetaAI.Position.Y, screenSize.Y),
            NormalizeValue(lastRaquetaAIPosition, screenSize.Y),
            NormalizeValue(raquetaPlayer.Position.Y, screenSize.Y),
            NormalizeValue(lastRaquetaPlayerPosition, screenSize.Y)
		};

		lastBallPosition = ball.Position;
		lastRaquetaAIPosition = raquetaAI.Position.Y;
		lastRaquetaPlayerPosition = raquetaPlayer.Position.Y;

		return sendData;
	}

	public override async void _Process(double delta)
	{
		if (!ready) return;
		if (!raquetaPlayer.ready) return;
		var inputData = new GetInputMessage()
		{
			eventType = EventType.getInput,
			envData = GetEnvData()
		};

		ResponseInput response = await clientRL.SendGenericData<GetInputMessage, ResponseInput>(inputData);
		raquetaPlayer.action = response.action;

		var rewardData = new SetRewardMessage()
		{
			eventType = EventType.setReward,
			reward = (float)delta,
			done = false
		};
		await clientRL.SendGenericData<SetRewardMessage, Response>(rewardData);
	}

	public static float NormalizeValue(float value, float max)
	{
		return (value / max - 0.5f) * 2;
	}

	public async void OnBallScore(bool leftSide)
	{
		if (!ready) return;

		lastBallPosition = ball.Position;
		lastRaquetaAIPosition = raquetaAI.Position.Y;
		lastRaquetaPlayerPosition = raquetaPlayer.Position.Y;

		float score = leftSide ? -20 : 100;

		var rewardData = new SetRewardMessage()
		{
			eventType = EventType.setReward,
			reward = score,
			done = true
		};

		GetTree().Paused = true;
		Response resp = await clientRL.SendGenericData<SetRewardMessage, Response>(rewardData);
		GetTree().Paused = false;
		GD.Print("Reward sent", resp);
	}
}
