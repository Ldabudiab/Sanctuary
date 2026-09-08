using Godot;

public partial class Chibi3DPlayer : CharacterBody3D
{
	[Export(PropertyHint.Range, "0.5,10,0.1")]
	public float MovementSpeed { get; set; } = 4.0f;

	[Export(PropertyHint.Range, "1,20,0.5")]
	public float FacingSmoothing { get; set; } = 10.0f;

	private float _gravity;

	public override void _Ready()
	{
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 input = Vector2.Zero;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			input.X -= 1.0f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			input.X += 1.0f;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			input.Y -= 1.0f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			input.Y += 1.0f;

		input = input.Normalized();
		Vector3 movement = new(input.X, 0.0f, input.Y);
		Camera3D camera = GetViewport().GetCamera3D();
		if (IsInstanceValid(camera))
		{
			Vector3 cameraRight = camera.GlobalBasis.X;
			Vector3 cameraForward = -camera.GlobalBasis.Z;
			cameraRight.Y = 0.0f;
			cameraForward.Y = 0.0f;
			cameraRight = cameraRight.Normalized();
			cameraForward = cameraForward.Normalized();
			movement = ((cameraRight * input.X) + (cameraForward * -input.Y)).Normalized();
		}
		Velocity = new Vector3(
			movement.X * MovementSpeed,
			IsOnFloor() ? -0.1f : Velocity.Y - (_gravity * (float)delta),
			movement.Z * MovementSpeed);

		if (movement.LengthSquared() > 0.001f)
		{
			float targetYaw = Mathf.Atan2(-movement.X, -movement.Z);
			float blend = 1.0f - Mathf.Exp(-FacingSmoothing * (float)delta);
			Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, targetYaw, blend), Rotation.Z);
		}

		MoveAndSlide();
	}
}
