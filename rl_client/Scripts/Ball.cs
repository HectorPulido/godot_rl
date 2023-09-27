using System.Threading.Tasks;
using Godot;
using System;

public partial class Ball : Area2D
{
	[Export]
	public float speed = 400;

	public Vector2 velocity = new();
	Vector2 screenSize;

	[Signal]
	public delegate void ScoreEventHandler(bool leftSide);

	public override void _Ready()
	{
		screenSize = GetViewportRect().Size;
		Start();
	}

	private async void Start()
	{
		Position = screenSize / 2;
		velocity = Vector2.Zero;

		GetTree().CallGroup("racket", nameof(Raqueta.SetReadyValue), false);
		GetTree().CallGroup("racket", nameof(Raqueta.SetInStart));
		await Task.Delay(50); // 2 seconds

		GetTree().CallGroup("racket", nameof(Raqueta.SetReadyValue), true);

		Random random = new();
		var startX = -1;//random.Next() % 2 == 0 ? 1 : -1;
		var startY = (float) random.NextDouble() * 2 - 1;
		var tempVelocity = new Vector2(startX, startY);
		tempVelocity = tempVelocity.Normalized() * speed;

		velocity = tempVelocity;
	}

	public void Bounce(float speed)
	{
		var tempVelociy = new Vector2(-velocity.X, speed);
		velocity = tempVelociy.Normalized() * this.speed;
	}

	public override void _Process(double delta)
	{
		if (Position.X > screenSize.X || Position.X < 0)
		{
			EmitSignal(SignalName.Score, Position.X < 0);
			Start();
		}
		if (Position.Y > screenSize.Y || Position.Y < 0)
		{
			velocity = new Vector2(velocity.X, -velocity.Y);
		}
		Position += velocity * (float)delta;
	}

	private void OnBallBodyEntered(Node2D body)
	{
		if (body.GetType() != typeof(Raqueta))
		{
			return;
		}
		var racket = (Raqueta)body;
		Bounce(racket.velocityY);
	}
}
