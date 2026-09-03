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

	[Export(PropertyHint.Range, "0,100,1")]
	public float WakeEnergyThreshold { get; set; } = 85.0f;

	public string CurrentAiState { get; private set; } = "Idle";

	private readonly RandomNumberGenerator _random = new();
	private Node2D _visual = null!;
	private Polygon2D _neutralMark = null!;
	private Polygon2D _heart = null!;
	private Polygon2D _eatingMark = null!;
	private Label _sleepMark = null!;
	private CreatureNeeds _needs = null!;
	private CreaturePersonality _personality = null!;
	private Player _player = null!;
	private Vector2 _wanderDirection;
	private float _stateTimeRemaining;
	private float _reactionTimeRemaining;
	private float _reactionElapsed;
	private bool _isWandering;
	private bool _isSleeping;
	private Reaction _reaction;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_neutralMark = GetNode<Polygon2D>("NeutralIndicator/NeutralMark");
		_heart = GetNode<Polygon2D>("NeutralIndicator/Heart");
		_eatingMark = GetNode<Polygon2D>("NeutralIndicator/EatingMark");
		_sleepMark = GetNode<Label>("NeutralIndicator/SleepMark");
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
			_needs.TickAwake((float)delta);
			UpdateReaction((float)delta);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (_isSleeping)
		{
			_needs.TickSleeping((float)delta);
			Velocity = Vector2.Zero;

			if (_needs.Energy >= WakeEnergyThreshold)
				WakeUp();

			MoveAndSlide();
			return;
		}

		_needs.TickAwake((float)delta);

		_stateTimeRemaining -= (float)delta;

		if (_stateTimeRemaining <= 0.0f)
		{
			if (ShouldSleep())
				BeginSleep();
			else if (_isWandering)
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

		if (_isSleeping)
			WakeUp();

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
		CurrentAiState = reaction == Reaction.Petting ? "Petting" : "Eating";
		_reactionTimeRemaining = duration;
		_reactionElapsed = 0.0f;
		_neutralMark.Visible = false;
		_heart.Visible = reaction == Reaction.Petting;
		_eatingMark.Visible = reaction == Reaction.Eating;
	}

	private bool ShouldSleep()
	{
		float sleepChance = 1.0f / (1.0f + Mathf.Exp((_needs.Energy - 30.0f) / 8.0f));
		sleepChance = Mathf.Clamp(sleepChance, 0.01f, 0.95f);
		return _random.Randf() < sleepChance;
	}

	private void BeginSleep()
	{
		_isSleeping = true;
		_isWandering = false;
		CurrentAiState = "Sleep";
		_neutralMark.Visible = false;
		_heart.Visible = false;
		_eatingMark.Visible = false;
		_sleepMark.Visible = true;
	}

	private void WakeUp()
	{
		_isSleeping = false;
		_sleepMark.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
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
		CurrentAiState = "Idle";
		float durationMultiplier = _personality.Activity >= 0.0f
			? Mathf.Lerp(1.0f, 0.35f, _personality.Activity / 100.0f)
			: Mathf.Lerp(1.0f, 3.0f, -_personality.Activity / 100.0f);
		_stateTimeRemaining = _random.RandfRange(IdleDurationRange.X, IdleDurationRange.Y) * durationMultiplier;
	}

	private void BeginWandering()
	{
		_isWandering = true;
		float durationMultiplier = _personality.Activity >= 0.0f
			? Mathf.Lerp(1.0f, 2.5f, _personality.Activity / 100.0f)
			: Mathf.Lerp(1.0f, 0.35f, -_personality.Activity / 100.0f);
		_stateTimeRemaining = _random.RandfRange(WanderDurationRange.X, WanderDurationRange.Y) * durationMultiplier;

		float approachChance = _personality.Attachment >= 0.0f
			? Mathf.Lerp(0.08f, 0.45f, _personality.Attachment / 100.0f)
			: Mathf.Lerp(0.08f, 0.005f, -_personality.Attachment / 100.0f);
		bool playerIsNearby = _player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) <= PlayerApproachRange;

		if (playerIsNearby && _random.Randf() < approachChance)
		{
			CurrentAiState = "ApproachPlayer";
			_wanderDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
		}
		else
		{
			CurrentAiState = "Wander";
			_wanderDirection = Vector2.Right.Rotated(_random.RandfRange(0.0f, Mathf.Tau));
		}
	}
}
