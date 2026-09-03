using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MovementSpeed { get; set; } = 200.0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Vector2.Zero;

		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			inputDirection.X -= 1.0f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			inputDirection.X += 1.0f;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			inputDirection.Y -= 1.0f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			inputDirection.Y += 1.0f;

		Velocity = inputDirection.Normalized() * MovementSpeed;
		MoveAndSlide();
	}
}
