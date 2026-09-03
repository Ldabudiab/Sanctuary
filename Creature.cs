using Godot;

public partial class Creature : CharacterBody2D
{
	[Export]
	public float WanderSpeed { get; set; } = 70.0f;

	[Export]
	public Vector2 IdleDurationRange { get; set; } = new(1.0f, 2.5f);

	[Export]
	public Vector2 WanderDurationRange { get; set; } = new(1.0f, 2.0f);

	private readonly RandomNumberGenerator _random = new();
	private Vector2 _wanderDirection;
	private float _stateTimeRemaining;
	private bool _isWandering;

	public override void _Ready()
	{
		_random.Randomize();
		BeginIdle();
	}

	public override void _PhysicsProcess(double delta)
	{
		_stateTimeRemaining -= (float)delta;

		if (_stateTimeRemaining <= 0.0f)
		{
			if (_isWandering)
				BeginIdle();
			else
				BeginWandering();
		}

		Velocity = _isWandering ? _wanderDirection * WanderSpeed : Vector2.Zero;
		MoveAndSlide();
	}

	private void BeginIdle()
	{
		_isWandering = false;
		_stateTimeRemaining = _random.RandfRange(IdleDurationRange.X, IdleDurationRange.Y);
	}

	private void BeginWandering()
	{
		_isWandering = true;
		_stateTimeRemaining = _random.RandfRange(WanderDurationRange.X, WanderDurationRange.Y);
		_wanderDirection = Vector2.Right.Rotated(_random.RandfRange(0.0f, Mathf.Tau));
	}
}
