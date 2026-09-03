using Godot;

public partial class Creature : CharacterBody2D, IInteractable
{
	[Export]
	public float WanderSpeed { get; set; } = 70.0f;

	[Export]
	public Vector2 IdleDurationRange { get; set; } = new(1.0f, 2.5f);

	[Export]
	public Vector2 WanderDurationRange { get; set; } = new(1.0f, 2.0f);

	[Export]
	public float PetReactionDuration { get; set; } = 1.5f;

	private readonly RandomNumberGenerator _random = new();
	private Node2D _visual = null!;
	private Polygon2D _neutralMark = null!;
	private Polygon2D _heart = null!;
	private CreatureNeeds _needs = null!;
	private Vector2 _wanderDirection;
	private float _stateTimeRemaining;
	private float _petTimeRemaining;
	private float _petElapsed;
	private bool _isWandering;
	private bool _isBeingPetted;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_neutralMark = GetNode<Polygon2D>("NeutralIndicator/NeutralMark");
		_heart = GetNode<Polygon2D>("NeutralIndicator/Heart");
		_needs = GetNode<CreatureNeeds>("Needs");
		_random.Randomize();
		BeginIdle();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isBeingPetted)
		{
			UpdatePetReaction((float)delta);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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

	public bool TryInteract(Node interactor)
	{
		if (_isBeingPetted)
			return false;

		_isBeingPetted = true;
		_isWandering = false;
		_petTimeRemaining = PetReactionDuration;
		_petElapsed = 0.0f;
		_neutralMark.Visible = false;
		_heart.Visible = true;
		_needs.ApplyPetting();
		return true;
	}

	private void UpdatePetReaction(float delta)
	{
		_petTimeRemaining -= delta;
		_petElapsed += delta;
		_visual.Position = Vector2.Up * Mathf.Abs(Mathf.Sin(_petElapsed * Mathf.Tau * 1.5f)) * 5.0f;

		if (_petTimeRemaining > 0.0f)
			return;

		_isBeingPetted = false;
		_visual.Position = Vector2.Zero;
		_heart.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
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
