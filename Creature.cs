using Godot;

public partial class Creature : CharacterBody2D, IInteractable
{
	private enum Reaction
	{
		None,
		Petting,
		Eating
	}

	[Export]
	public float WanderSpeed { get; set; } = 70.0f;

	[Export]
	public Vector2 IdleDurationRange { get; set; } = new(1.0f, 2.5f);

	[Export]
	public Vector2 WanderDurationRange { get; set; } = new(1.0f, 2.0f);

	[Export]
	public float PetReactionDuration { get; set; } = 1.5f;

	[Export]
	public float EatReactionDuration { get; set; } = 2.0f;

	[Export]
	public float PlayerApproachRange { get; set; } = 320.0f;

	private readonly RandomNumberGenerator _random = new();
	private Node2D _visual = null!;
	private Polygon2D _neutralMark = null!;
	private Polygon2D _heart = null!;
	private Polygon2D _eatingMark = null!;
	private CreatureNeeds _needs = null!;
	private CreaturePersonality _personality = null!;
	private Player _player = null!;
	private Vector2 _wanderDirection;
	private float _stateTimeRemaining;
	private float _reactionTimeRemaining;
	private float _reactionElapsed;
	private bool _isWandering;
	private Reaction _reaction;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_neutralMark = GetNode<Polygon2D>("NeutralIndicator/NeutralMark");
		_heart = GetNode<Polygon2D>("NeutralIndicator/Heart");
		_eatingMark = GetNode<Polygon2D>("NeutralIndicator/EatingMark");
		_needs = GetNode<CreatureNeeds>("Needs");
		_personality = GetNode<CreaturePersonality>("Personality");
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_random.Randomize();
		BeginIdle();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_reaction != Reaction.None)
		{
			UpdateReaction((float)delta);
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
		if (_reaction != Reaction.None || interactor is not Player player)
			return false;

		if (player.IsCarryingFood)
		{
			if (!player.TryConsumeCarriedFood())
				return false;

			_needs.ApplyFeeding();
			_personality.ApplyFeeding();
			BeginReaction(Reaction.Eating, EatReactionDuration);
		}
		else
		{
			_needs.ApplyPetting();
			_personality.ApplyPetting();
			BeginReaction(Reaction.Petting, PetReactionDuration);
		}

		return true;
	}

	private void BeginReaction(Reaction reaction, float duration)
	{
		_reaction = reaction;
		_isWandering = false;
		_reactionTimeRemaining = duration;
		_reactionElapsed = 0.0f;
		_neutralMark.Visible = false;
		_heart.Visible = reaction == Reaction.Petting;
		_eatingMark.Visible = reaction == Reaction.Eating;
	}

	private void UpdateReaction(float delta)
	{
		_reactionTimeRemaining -= delta;
		_reactionElapsed += delta;
		_visual.Position = Vector2.Up * Mathf.Abs(Mathf.Sin(_reactionElapsed * Mathf.Tau * 1.5f)) * 5.0f;

		if (_reactionTimeRemaining > 0.0f)
			return;

		_reaction = Reaction.None;
		_visual.Position = Vector2.Zero;
		_heart.Visible = false;
		_eatingMark.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private void BeginIdle()
	{
		_isWandering = false;
		float energyWeight = (_personality.Energy + 100.0f) / 200.0f;
		float durationMultiplier = Mathf.Lerp(1.5f, 0.6f, energyWeight);
		_stateTimeRemaining = _random.RandfRange(IdleDurationRange.X, IdleDurationRange.Y) * durationMultiplier;
	}

	private void BeginWandering()
	{
		_isWandering = true;
		float energyWeight = (_personality.Energy + 100.0f) / 200.0f;
		float durationMultiplier = Mathf.Lerp(0.6f, 1.5f, energyWeight);
		_stateTimeRemaining = _random.RandfRange(WanderDurationRange.X, WanderDurationRange.Y) * durationMultiplier;

		float attachmentWeight = (_personality.Attachment + 100.0f) / 200.0f;
		float approachChance = Mathf.Lerp(0.03f, 0.35f, attachmentWeight);
		bool playerIsNearby = _player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) <= PlayerApproachRange;

		if (playerIsNearby && _random.Randf() < approachChance)
			_wanderDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
		else
			_wanderDirection = Vector2.Right.Rotated(_random.RandfRange(0.0f, Mathf.Tau));
	}
}
