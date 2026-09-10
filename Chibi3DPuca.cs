using Godot;

public partial class Chibi3DPuca : CharacterBody3D
{
	private static readonly StringName WalkAnimationName = "Puca_Walk_Manual";

	private enum PucaState
	{
		Idle,
		Wander
	}

	[ExportGroup("Movement")]
	[Export(PropertyHint.Range, "0.1,5,0.05")]
	public float WalkSpeed { get; set; } = 1.15f;

	[Export(PropertyHint.Range, "45,720,5")]
	public float TurnSpeedDegrees { get; set; } = 180.0f;

	[Export(PropertyHint.Range, "0.05,2,0.05")]
	public float ArrivalDistance { get; set; } = 0.25f;

	[ExportGroup("Timing")]
	[Export]
	public Vector2 IdleTimeRange { get; set; } = new(1.5f, 4.0f);

	[ExportGroup("Roaming")]
	[Export]
	public Vector2 RoamingMinimum { get; set; } = new(-8.75f, -6.75f);

	[Export]
	public Vector2 RoamingMaximum { get; set; } = new(8.75f, 6.75f);

	[Export(PropertyHint.Range, "0.5,8,0.1")]
	public float MinimumWanderDistance { get; set; } = 1.5f;

	[Export(PropertyHint.Range, "0.5,8,0.1")]
	public float MaximumWanderDistance { get; set; } = 4.0f;

	[Export(PropertyHint.Range, "0.25,5,0.05")]
	public float BlockedTimeout { get; set; } = 1.25f;

	[ExportGroup("Model Facing")]
	[Export(PropertyHint.Range, "-180,180,1")]
	public float ModelForwardYawDegrees { get; set; } = 0.0f;

	[ExportGroup("Runtime Debug")]
	[Export]
	public string CurrentState { get; set; } = "Idle";

	[Export]
	public Vector3 CurrentTarget { get; set; }

	private readonly RandomNumberGenerator _random = new();
	private Node3D _visualRoot = null!;
	private AnimationPlayer _animationPlayer = null!;
	private bool _animationAvailable;
	private bool _walkAnimationActive;
	private PucaState _state;
	private float _stateTimeRemaining;
	private float _blockedTime;
	private float _previousTargetDistance = float.MaxValue;
	private float _gravity;

	public override void _Ready()
	{
		_visualRoot = GetNode<Node3D>("VisualRoot");
		InitializeAnimation();
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
		_random.Randomize();
		BeginIdle();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		float verticalVelocity = IsOnFloor() ? -0.1f : Velocity.Y - (_gravity * step);

		if (_state == PucaState.Idle)
		{
			Velocity = new Vector3(0.0f, verticalVelocity, 0.0f);
			_stateTimeRemaining -= step;
			if (_stateTimeRemaining <= 0.0f)
				BeginWander();
		}
		else
		{
			UpdateWander(step, verticalVelocity);
		}

		bool isMoving = _state == PucaState.Wander
			&& new Vector2(Velocity.X, Velocity.Z).LengthSquared() > 0.0001f;
		SetWalkAnimationActive(isMoving);

		MoveAndSlide();
	}

	private void InitializeAnimation()
	{
		_animationPlayer = _visualRoot.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
		if (_animationPlayer == null)
		{
			GD.PushWarning("Chibi3DPuca could not find the imported AnimationPlayer.");
			return;
		}

		if (!_animationPlayer.HasAnimation(WalkAnimationName))
		{
			GD.PushWarning($"Chibi3DPuca is missing animation '{WalkAnimationName}'.");
			return;
		}

		// The GLB animation is authored in-place but is not flagged to loop in the
		// imported file. Loop it at runtime without changing the source animation.
		Animation walkAnimation = _animationPlayer.GetAnimation(WalkAnimationName);
		walkAnimation.LoopMode = Animation.LoopModeEnum.Linear;
		_animationAvailable = true;
		ApplyNeutralPose();
	}

	private void SetWalkAnimationActive(bool shouldPlay)
	{
		if (!_animationAvailable || shouldPlay == _walkAnimationActive)
			return;

		_walkAnimationActive = shouldPlay;
		if (shouldPlay)
		{
			_animationPlayer.Play(WalkAnimationName);
		}
		else
		{
			ApplyNeutralPose();
		}
	}

	private void ApplyNeutralPose()
	{
		if (!_animationAvailable)
			return;

		// Frame zero is the authored neutral/contact pose. Evaluate it once and
		// pause there so Idle never freezes on an arbitrary mid-step frame.
		_animationPlayer.Play(WalkAnimationName);
		_animationPlayer.Seek(0.0, true);
		_animationPlayer.Pause();
		_walkAnimationActive = false;
	}

	private void BeginIdle()
	{
		_state = PucaState.Idle;
		CurrentState = "Idle";
		Velocity = Vector3.Zero;
		_stateTimeRemaining = _random.RandfRange(
			Mathf.Min(IdleTimeRange.X, IdleTimeRange.Y),
			Mathf.Max(IdleTimeRange.X, IdleTimeRange.Y));
		_blockedTime = 0.0f;
		_previousTargetDistance = float.MaxValue;
	}

	private void BeginWander()
	{
		Vector2 current = new(GlobalPosition.X, GlobalPosition.Z);
		float minimumDistance = Mathf.Min(MinimumWanderDistance, MaximumWanderDistance);
		float maximumDistance = Mathf.Max(MinimumWanderDistance, MaximumWanderDistance);
		Vector2 target = current;

		for (int attempt = 0; attempt < 12; attempt++)
		{
			float angle = _random.RandfRange(0.0f, Mathf.Tau);
			float distance = _random.RandfRange(minimumDistance, maximumDistance);
			Vector2 candidate = current + Vector2.Right.Rotated(angle) * distance;
			candidate.X = Mathf.Clamp(candidate.X, RoamingMinimum.X, RoamingMaximum.X);
			candidate.Y = Mathf.Clamp(candidate.Y, RoamingMinimum.Y, RoamingMaximum.Y);
			if (candidate.DistanceTo(current) >= minimumDistance * 0.75f)
			{
				target = candidate;
				break;
			}
		}

		CurrentTarget = new Vector3(target.X, GlobalPosition.Y, target.Y);
		_state = PucaState.Wander;
		CurrentState = "Wander";
		_blockedTime = 0.0f;
		_previousTargetDistance = HorizontalDistanceToTarget();
	}

	private void UpdateWander(float delta, float verticalVelocity)
	{
		Vector3 toTarget = CurrentTarget - GlobalPosition;
		toTarget.Y = 0.0f;
		float distance = toTarget.Length();
		if (distance <= ArrivalDistance)
		{
			BeginIdle();
			Velocity = new Vector3(0.0f, verticalVelocity, 0.0f);
			return;
		}

		Vector3 direction = toTarget / distance;
		Velocity = new Vector3(direction.X * WalkSpeed, verticalVelocity, direction.Z * WalkSpeed);
		TurnVisualToward(direction, delta);

		if (distance < _previousTargetDistance - 0.01f)
			_blockedTime = 0.0f;
		else
			_blockedTime += delta;

		_previousTargetDistance = distance;
		if (_blockedTime >= BlockedTimeout)
			BeginIdle();
	}

	private void TurnVisualToward(Vector3 direction, float delta)
	{
		// The imported GLB's corrected forward is local +Z. Convert the desired
		// world yaw into VisualRoot-local space so the prototype root's placement
		// rotation does not offset the visible facing direction.
		float desiredWorldYaw = Mathf.Atan2(direction.X, direction.Z);
		float targetLocalYaw = desiredWorldYaw - GlobalRotation.Y
			+ Mathf.DegToRad(ModelForwardYawDegrees);
		float currentYaw = _visualRoot.Rotation.Y;
		float difference = Mathf.Wrap(targetLocalYaw - currentYaw, -Mathf.Pi, Mathf.Pi);
		float maximumTurn = Mathf.DegToRad(TurnSpeedDegrees) * delta;
		float nextYaw = currentYaw + Mathf.Clamp(difference, -maximumTurn, maximumTurn);
		_visualRoot.Rotation = new Vector3(
			_visualRoot.Rotation.X,
			nextYaw,
			_visualRoot.Rotation.Z);
	}

	private float HorizontalDistanceToTarget()
	{
		Vector3 offset = CurrentTarget - GlobalPosition;
		offset.Y = 0.0f;
		return offset.Length();
	}
}
