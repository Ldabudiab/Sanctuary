using Godot;

public partial class Chibi3DCamera : Node3D
{
	[Export]
	public NodePath TargetPath { get; set; } = null!;

	[Export]
	public Vector3 FollowOffset { get; set; } = new(0.0f, 7.5f, 9.0f);

	[Export(PropertyHint.Range, "20,80,1")]
	public float FieldOfView { get; set; } = 42.0f;

	[Export(PropertyHint.Range, "0,20,0.25")]
	public float FollowSmoothing { get; set; } = 5.0f;

	[Export(PropertyHint.Range, "0,4,0.1")]
	public float LookAtHeight { get; set; } = 0.85f;

	private Node3D _target = null!;
	private Camera3D _camera = null!;

	public override void _Ready()
	{
		_target = GetNode<Node3D>(TargetPath);
		_camera = GetNode<Camera3D>("Camera3D");
		_camera.Fov = FieldOfView;
		GlobalPosition = _target.GlobalPosition + FollowOffset;
		AimCamera();
	}

	public override void _Process(double delta)
	{
		Vector3 desiredPosition = _target.GlobalPosition + FollowOffset;
		float blend = FollowSmoothing <= 0.0f
			? 1.0f
			: 1.0f - Mathf.Exp(-FollowSmoothing * (float)delta);
		GlobalPosition = GlobalPosition.Lerp(desiredPosition, blend);
		_camera.Fov = FieldOfView;
		AimCamera();
	}

	private void AimCamera()
	{
		LookAt(_target.GlobalPosition + (Vector3.Up * LookAtHeight), Vector3.Up);
	}
}
