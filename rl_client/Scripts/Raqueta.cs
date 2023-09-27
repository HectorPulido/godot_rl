using Godot;

public partial class Raqueta : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 400;
	[Export]
	public bool IsAI { get; set; } = false;

	public int action = 0;

	public float velocityY = 0;
	private Vector2 screenSize = new();
	private Ball ball;
	private float initialPositionX;
	public bool ready = false;

	public override void _Ready()
	{
		screenSize = GetViewportRect().Size;
        
        if (IsAI)
		{
			ball = GetNode<Ball>("../Ball");
			initialPositionX = 0.9f;
		}
		else
		{
			initialPositionX = 0.1f;
		}
		SetInStart();
	}

	public void SetReadyValue(bool value)
	{
		ready = value;
	}
	
	public void SetInStart()
	{
		velocityY = 0;
		Position = new Vector2(screenSize.X * initialPositionX, screenSize.Y / 2);
	}

	private void HandleAI()
	{
		var direction = ball.Position.Y - Position.Y;
		direction = Mathf.Clamp(direction, -1, 1);
		velocityY = direction * Speed;
	}

	private void HandlePlayer()
	{
		velocityY = 0;
		if (action == 1)
		{
			velocityY = -Speed;
		}
		if (action == 2)
		{
			velocityY = Speed;
		}
	}

	public override void _Process(double delta)
	{
		if (!ready) return;

		if (IsAI)
		{
			HandleAI();
		}
		else
		{
			HandlePlayer();
		}

		var tempPosition = velocityY * (float)delta + Position.Y;
		tempPosition = Mathf.Clamp(tempPosition, 0, screenSize.Y);
		Position = new Vector2(Position.X, tempPosition);
	}
}
