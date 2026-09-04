using Godot;

public partial class Creature : CharacterBody2D, IInteractable
{
	private enum Reaction
	{
		None,
		Petting,
		Eating,
		StatBoost
	}

	private enum SocialInteraction
	{
		None,
		Greeting,
		Push
	}

	private enum CompetitionMode
	{
		None,
		Race,
		Fight
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

	[Export]
	public float InterestPointRange { get; set; } = 420.0f;

	[Export]
	public float InvestigationArrivalDistance { get; set; } = 28.0f;

	[Export]
	public float InvestigationDuration { get; set; } = 1.5f;

	[Export]
	public float SocialInteractionRange { get; set; } = 400.0f;

	[Export]
	public float SocialArrivalDistance { get; set; } = 42.0f;

	[Export]
	public float SocialReactionDuration { get; set; } = 1.5f;

	[Export]
	public float PushSpeed { get; set; } = 120.0f;

	[Export]
	public float PushDuration { get; set; } = 0.35f;

	[Export(PropertyHint.Range, "0,100,1")]
	public float RunUnlockSpeed { get; set; } = 20.0f;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float RunChance { get; set; } = 0.35f;

	[Export(PropertyHint.Range, "1,4,0.1")]
	public float RunSpeedMultiplier { get; set; } = 2.0f;

	public string CurrentAiState { get; private set; } = "Idle";
	public float CompetitionSpeed => _stats.GetValue(CreatureStatType.Speed);
	public float CompetitionPower => _stats.GetValue(CreatureStatType.Power);
	public bool CanBeginCompetition => _competitionMode == CompetitionMode.None;

	private readonly RandomNumberGenerator _random = new();
	private Node2D _visual = null!;
	private Polygon2D _neutralMark = null!;
	private Polygon2D _heart = null!;
	private Polygon2D _eatingMark = null!;
	private Label _sleepMark = null!;
	private Label _investigateMark = null!;
	private Label _socialHappyMark = null!;
	private Label _socialPushMark = null!;
	private Label _statBoostMark = null!;
	private CreatureNeeds _needs = null!;
	private CreaturePersonality _personality = null!;
	private CreatureStats _stats = null!;
	private Player _player = null!;
	private InterestPoint _investigationTarget = null!;
	private Creature _socialPartner = null!;
	private Vector2 _wanderDirection;
	private float _stateTimeRemaining;
	private float _reactionTimeRemaining;
	private float _reactionElapsed;
	private float _socialTimeRemaining;
	private float _pushTimeRemaining;
	private Vector2 _pushDirection;
	private bool _isWandering;
	private bool _isSleeping;
	private bool _isInvestigating;
	private bool _isExamining;
	private bool _isSocialInitiator;
	private bool _isSocialPerforming;
	private bool _isRunning;
	private bool _isRaceMoving;
	private Vector2 _raceFinishPosition;
	private float _raceMovementSpeed;
	private CompetitionMode _competitionMode;
	private float _fightAttackVisualTime;
	private float _fightHitVisualTime;
	private Vector2 _fightLungeDirection;
	private Reaction _reaction;
	private SocialInteraction _socialInteraction;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_neutralMark = GetNode<Polygon2D>("NeutralIndicator/NeutralMark");
		_heart = GetNode<Polygon2D>("NeutralIndicator/Heart");
		_eatingMark = GetNode<Polygon2D>("NeutralIndicator/EatingMark");
		_sleepMark = GetNode<Label>("NeutralIndicator/SleepMark");
		_investigateMark = GetNode<Label>("NeutralIndicator/InvestigateMark");
		_socialHappyMark = GetNode<Label>("NeutralIndicator/SocialHappyMark");
		_socialPushMark = GetNode<Label>("NeutralIndicator/SocialPushMark");
		_statBoostMark = GetNode<Label>("NeutralIndicator/StatBoostMark");
		_needs = GetNode<CreatureNeeds>("Needs");
		_personality = GetNode<CreaturePersonality>("Personality");
		_stats = GetNode<CreatureStats>("Stats");
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_random.Randomize();
		BeginIdle();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_competitionMode == CompetitionMode.Race)
		{
			_needs.TickAwake((float)delta);
			UpdateRaceMovement((float)delta);
			MoveAndSlide();
			return;
		}

		if (_competitionMode == CompetitionMode.Fight)
		{
			UpdateFightVisual((float)delta);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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

		if (_socialInteraction != SocialInteraction.None)
		{
			UpdateSocialInteraction((float)delta);
			MoveAndSlide();
			return;
		}

		if (_isInvestigating)
		{
			UpdateInvestigation((float)delta);
			MoveAndSlide();
			return;
		}

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

		Velocity = _isWandering ? _wanderDirection * GetMovementSpeed() : Vector2.Zero;
		MoveAndSlide();
	}

	public bool TryInteract(Node interactor)
	{
		if (_competitionMode != CompetitionMode.None || _reaction != Reaction.None || interactor is not Player player)
			return false;

		if (_isSleeping)
			WakeUp();

		if (player.CarriedItem != null)
		{
			if (!player.TryTakeCarriedItem(out CarriedItem item))
				return false;

			if (item.Kind == CarriedItemKind.Food)
			{
				_needs.ApplyFeeding();
				_personality.ApplyFeeding();
				BeginReaction(Reaction.Eating, EatReactionDuration);
			}
			else if (item.Kind == CarriedItemKind.Crystal && item.StatType.HasValue)
			{
				_stats.ApplyIncrease(item.StatType.Value, item.StatIncrease);
				BeginReaction(Reaction.StatBoost, PetReactionDuration);
			}
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
		CancelSocialInteraction();
		CancelInvestigation();
		_reaction = reaction;
		_isWandering = false;
		_isRunning = false;
		CurrentAiState = reaction switch
		{
			Reaction.Petting => "Petting",
			Reaction.Eating => "Eating",
			_ => "StatBoost"
		};
		_reactionTimeRemaining = duration;
		_reactionElapsed = 0.0f;
		_neutralMark.Visible = false;
		_heart.Visible = reaction == Reaction.Petting;
		_eatingMark.Visible = reaction == Reaction.Eating;
		_statBoostMark.Visible = reaction == Reaction.StatBoost;
	}

	private bool ShouldSleep()
	{
		float sleepChance = 1.0f / (1.0f + Mathf.Exp((_needs.Energy - 30.0f) / 8.0f));
		sleepChance = Mathf.Clamp(sleepChance, 0.01f, 0.95f);
		return _random.Randf() < sleepChance;
	}

	private void BeginSleep()
	{
		CancelSocialInteraction();
		CancelInvestigation();
		_isSleeping = true;
		_isWandering = false;
		_isRunning = false;
		CurrentAiState = "Sleep";
		_neutralMark.Visible = false;
		_heart.Visible = false;
		_eatingMark.Visible = false;
		_statBoostMark.Visible = false;
		_sleepMark.Visible = true;
	}

	private void WakeUp()
	{
		_isSleeping = false;
		_sleepMark.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private bool TryBeginInvestigation()
	{
		InterestPoint closestPoint = null;
		float closestDistanceSquared = InterestPointRange * InterestPointRange;

		foreach (Node node in GetTree().GetNodesInGroup("interest_points"))
		{
			if (node is not InterestPoint point)
				continue;

			float distanceSquared = GlobalPosition.DistanceSquaredTo(point.InvestigationPosition);
			if (distanceSquared <= closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestPoint = point;
			}
		}

		if (closestPoint == null)
			return false;

		float investigationChance = _personality.Curiosity >= 0.0f
			? Mathf.Lerp(0.06f, 0.40f, _personality.Curiosity / 100.0f)
			: Mathf.Lerp(0.06f, 0.005f, -_personality.Curiosity / 100.0f);

		if (_random.Randf() >= investigationChance)
			return false;

		_investigationTarget = closestPoint;
		_isInvestigating = true;
		_isExamining = false;
		_isWandering = false;
		ChooseMovementMode();
		SetMovementState("Investigate");
		_neutralMark.Visible = false;
		_investigateMark.Visible = true;
		return true;
	}

	private void UpdateInvestigation(float delta)
	{
		if (!IsInstanceValid(_investigationTarget))
		{
			FinishInvestigation();
			return;
		}

		if (!_isExamining)
		{
			Vector2 targetPosition = _investigationTarget.InvestigationPosition;
			if (GlobalPosition.DistanceTo(targetPosition) > InvestigationArrivalDistance)
			{
				Velocity = GlobalPosition.DirectionTo(targetPosition) * GetMovementSpeed();
				return;
			}

			_isExamining = true;
			_isRunning = false;
			CurrentAiState = "Investigate";
			_stateTimeRemaining = InvestigationDuration;
			Velocity = Vector2.Zero;
		}

		_stateTimeRemaining -= delta;
		if (_stateTimeRemaining <= 0.0f)
			FinishInvestigation();
	}

	private void FinishInvestigation()
	{
		CancelInvestigation();
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private void CancelInvestigation()
	{
		_isInvestigating = false;
		_isExamining = false;
		_isRunning = false;
		_investigationTarget = null;
		_investigateMark.Visible = false;
		Velocity = Vector2.Zero;
	}

	private bool TryBeginSocialInteraction()
	{
		Creature closestCreature = null;
		float closestDistanceSquared = SocialInteractionRange * SocialInteractionRange;

		foreach (Node node in GetTree().GetNodesInGroup("creatures"))
		{
			if (node is not Creature creature || creature == this || !creature.CanParticipateInSocial())
				continue;

			float distanceSquared = GlobalPosition.DistanceSquaredTo(creature.GlobalPosition);
			if (distanceSquared <= closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestCreature = creature;
			}
		}

		if (closestCreature == null)
			return false;

		float socialChance = _personality.Social >= 0.0f
			? Mathf.Lerp(0.08f, 0.50f, _personality.Social / 100.0f)
			: Mathf.Lerp(0.08f, 0.005f, -_personality.Social / 100.0f);

		if (_random.Randf() >= socialChance)
			return false;

		float pushChance = _personality.Temperament >= 0.0f
			? Mathf.Lerp(0.35f, 0.90f, _personality.Temperament / 100.0f)
			: Mathf.Lerp(0.35f, 0.05f, -_personality.Temperament / 100.0f);
		SocialInteraction interaction = _random.Randf() < pushChance
			? SocialInteraction.Push
			: SocialInteraction.Greeting;

		if (!closestCreature.TryReserveSocialInteraction(this, interaction))
			return false;

		_socialPartner = closestCreature;
		_socialInteraction = interaction;
		_isSocialInitiator = true;
		_isSocialPerforming = false;
		_isWandering = false;
		ChooseMovementMode();
		SetMovementState(interaction == SocialInteraction.Greeting ? "Greeting" : "Pushing");
		return true;
	}

	private bool CanParticipateInSocial()
	{
		return _competitionMode == CompetitionMode.None
			&& _reaction == Reaction.None
			&& !_isSleeping
			&& !_isInvestigating
			&& _socialInteraction == SocialInteraction.None;
	}

	private bool TryReserveSocialInteraction(Creature initiator, SocialInteraction interaction)
	{
		if (!CanParticipateInSocial())
			return false;

		_socialPartner = initiator;
		_socialInteraction = interaction;
		_isSocialInitiator = false;
		_isSocialPerforming = false;
		_isWandering = false;
		_isRunning = false;
		Velocity = Vector2.Zero;
		CurrentAiState = interaction == SocialInteraction.Greeting ? "Greeting" : "BeingPushed";
		return true;
	}

	private void UpdateSocialInteraction(float delta)
	{
		if (!IsInstanceValid(_socialPartner) || _socialPartner._socialPartner != this)
		{
			FinishSocialInteraction(false);
			return;
		}

		if (!_isSocialPerforming)
		{
			if (!_isSocialInitiator)
			{
				Velocity = Vector2.Zero;
				return;
			}

			if (GlobalPosition.DistanceTo(_socialPartner.GlobalPosition) > SocialArrivalDistance)
			{
				Velocity = GlobalPosition.DirectionTo(_socialPartner.GlobalPosition) * GetMovementSpeed();
				return;
			}

			BeginSocialPerformance();
			_socialPartner.BeginSocialPerformance();
		}

		_socialTimeRemaining -= delta;
		Velocity = Vector2.Zero;

		if (_socialInteraction == SocialInteraction.Push && !_isSocialInitiator && _pushTimeRemaining > 0.0f)
		{
			_pushTimeRemaining -= delta;
			Velocity = _pushDirection * PushSpeed;
		}

		if (_socialTimeRemaining <= 0.0f)
			FinishSocialInteraction(true);
	}

	private void BeginSocialPerformance()
	{
		_isSocialPerforming = true;
		_isRunning = false;
		CurrentAiState = _socialInteraction == SocialInteraction.Greeting
			? "Greeting"
			: (_isSocialInitiator ? "Pushing" : "BeingPushed");
		_socialTimeRemaining = SocialReactionDuration;
		_socialHappyMark.Visible = _socialInteraction == SocialInteraction.Greeting;
		_socialPushMark.Visible = _socialInteraction == SocialInteraction.Push;
		_neutralMark.Visible = false;

		if (_socialInteraction == SocialInteraction.Push && !_isSocialInitiator)
		{
			_pushTimeRemaining = PushDuration;
			_pushDirection = _socialPartner.GlobalPosition.DirectionTo(GlobalPosition);
			if (_pushDirection == Vector2.Zero)
				_pushDirection = Vector2.Right;
		}
	}

	private void CancelSocialInteraction()
	{
		if (_socialInteraction != SocialInteraction.None)
			FinishSocialInteraction(true);
	}

	private void FinishSocialInteraction(bool notifyPartner)
	{
		Creature partner = _socialPartner;
		_socialPartner = null;
		_socialInteraction = SocialInteraction.None;
		_isSocialInitiator = false;
		_isSocialPerforming = false;
		_isRunning = false;
		_socialHappyMark.Visible = false;
		_socialPushMark.Visible = false;
		_neutralMark.Visible = true;
		Velocity = Vector2.Zero;
		BeginIdle();

		if (notifyPartner && IsInstanceValid(partner) && partner._socialPartner == this)
			partner.FinishSocialInteraction(false);
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
		_statBoostMark.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private void BeginIdle()
	{
		_isWandering = false;
		_isRunning = false;
		CurrentAiState = "Idle";
		float durationMultiplier = _personality.Activity >= 0.0f
			? Mathf.Lerp(1.0f, 0.35f, _personality.Activity / 100.0f)
			: Mathf.Lerp(1.0f, 3.0f, -_personality.Activity / 100.0f);
		_stateTimeRemaining = _random.RandfRange(IdleDurationRange.X, IdleDurationRange.Y) * durationMultiplier;
	}

	private void BeginWandering()
	{
		if (TryBeginSocialInteraction())
			return;

		if (TryBeginInvestigation())
			return;

		_isWandering = true;
		ChooseMovementMode();
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
			SetMovementState("ApproachPlayer");
			_wanderDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
		}
		else
		{
			SetMovementState("Wander");
			_wanderDirection = Vector2.Right.Rotated(_random.RandfRange(0.0f, Mathf.Tau));
		}
	}

	private void ChooseMovementMode()
	{
		_isRunning = _stats.Speed >= RunUnlockSpeed && _random.Randf() < RunChance;
	}

	private float GetMovementSpeed()
	{
		return WanderSpeed * (_isRunning ? RunSpeedMultiplier : 1.0f);
	}

	private void SetMovementState(string state)
	{
		CurrentAiState = $"{state} ({(_isRunning ? "Running" : "Walking")})";
	}

	public void PrepareForRace(Vector2 startPosition, Vector2 finishPosition, float movementSpeed)
	{
		if (!CanBeginCompetition)
			return;

		CancelSocialInteraction();
		CancelInvestigation();
		_reaction = Reaction.None;
		_isSleeping = false;
		_isWandering = false;
		_isRunning = false;
		_competitionMode = CompetitionMode.Race;
		_isRaceMoving = false;
		_raceFinishPosition = finishPosition;
		_raceMovementSpeed = movementSpeed;
		GlobalPosition = startPosition;
		Velocity = Vector2.Zero;
		_visual.Position = Vector2.Zero;
		HideTemporaryIndicators();
		CurrentAiState = "Race Countdown";
	}

	public void StartRaceMovement()
	{
		if (_competitionMode != CompetitionMode.Race)
			return;

		_isRaceMoving = true;
		CurrentAiState = "Racing";
	}

	public bool HasReachedRaceFinish()
	{
		return _competitionMode == CompetitionMode.Race && GlobalPosition.DistanceTo(_raceFinishPosition) <= 2.5f;
	}

	public void PauseRaceAtFinish(bool isWinner)
	{
		if (_competitionMode != CompetitionMode.Race)
			return;

		_isRaceMoving = false;
		Velocity = Vector2.Zero;
		CurrentAiState = isWinner ? "Race Winner" : "Race Finished";
	}

	public void EndRace()
	{
		if (_competitionMode != CompetitionMode.Race)
			return;

		_competitionMode = CompetitionMode.None;
		_isRaceMoving = false;
		Velocity = Vector2.Zero;
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private void UpdateRaceMovement(float delta)
	{
		if (!_isRaceMoving)
		{
			Velocity = Vector2.Zero;
			return;
		}

		float distanceToFinish = GlobalPosition.DistanceTo(_raceFinishPosition);
		if (distanceToFinish <= 2.5f)
		{
			GlobalPosition = _raceFinishPosition;
			Velocity = Vector2.Zero;
			return;
		}

		float frameLimitedSpeed = Mathf.Min(_raceMovementSpeed, distanceToFinish / Mathf.Max(delta, 0.0001f));
		Velocity = GlobalPosition.DirectionTo(_raceFinishPosition) * frameLimitedSpeed;
	}

	private void HideTemporaryIndicators()
	{
		_neutralMark.Visible = true;
		_heart.Visible = false;
		_eatingMark.Visible = false;
		_statBoostMark.Visible = false;
		_sleepMark.Visible = false;
		_investigateMark.Visible = false;
		_socialHappyMark.Visible = false;
		_socialPushMark.Visible = false;
	}

	public void PrepareForFight(Vector2 startPosition, Vector2 opponentPosition)
	{
		if (!CanBeginCompetition)
			return;

		CancelSocialInteraction();
		CancelInvestigation();
		_reaction = Reaction.None;
		_isSleeping = false;
		_isWandering = false;
		_isRunning = false;
		_competitionMode = CompetitionMode.Fight;
		GlobalPosition = startPosition;
		Velocity = Vector2.Zero;
		_visual.Position = Vector2.Zero;
		HideTemporaryIndicators();
		CurrentAiState = "Fighting";
		GetNode<CreatureVisualController>("Visual").FaceHorizontal(opponentPosition.X - startPosition.X);
	}

	public void ShowFightAttack(Vector2 opponentPosition)
	{
		if (_competitionMode != CompetitionMode.Fight)
			return;

		_fightLungeDirection = GlobalPosition.DirectionTo(opponentPosition);
		_fightAttackVisualTime = 0.3f;
	}

	public void ShowFightHit()
	{
		if (_competitionMode != CompetitionMode.Fight)
			return;

		_fightHitVisualTime = 0.35f;
		_socialPushMark.Visible = true;
		_neutralMark.Visible = false;
	}

	public void PauseFight(bool isWinner)
	{
		if (_competitionMode != CompetitionMode.Fight)
			return;

		_fightAttackVisualTime = 0.0f;
		_fightHitVisualTime = 0.0f;
		_visual.Position = Vector2.Zero;
		_socialPushMark.Visible = false;
		_neutralMark.Visible = true;
		CurrentAiState = isWinner ? "Fight Winner" : "Fight Finished";
	}

	public void EndFight()
	{
		if (_competitionMode != CompetitionMode.Fight)
			return;

		_competitionMode = CompetitionMode.None;
		_fightAttackVisualTime = 0.0f;
		_fightHitVisualTime = 0.0f;
		_visual.Position = Vector2.Zero;
		_socialPushMark.Visible = false;
		_neutralMark.Visible = true;
		BeginIdle();
	}

	private void UpdateFightVisual(float delta)
	{
		if (_fightAttackVisualTime > 0.0f)
		{
			_fightAttackVisualTime = Mathf.Max(0.0f, _fightAttackVisualTime - delta);
			float lunge = Mathf.Sin((_fightAttackVisualTime / 0.3f) * Mathf.Pi);
			_visual.Position = _fightLungeDirection * lunge * 6.0f;
		}
		else
		{
			_visual.Position = Vector2.Zero;
		}

		if (_fightHitVisualTime <= 0.0f)
			return;

		_fightHitVisualTime = Mathf.Max(0.0f, _fightHitVisualTime - delta);
		if (_fightHitVisualTime <= 0.0f)
		{
			_socialPushMark.Visible = false;
			_neutralMark.Visible = true;
		}
	}
}
