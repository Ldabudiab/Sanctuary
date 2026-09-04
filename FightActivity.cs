using Godot;

public partial class FightActivity : Area2D, IInteractable
{
	private enum FightState
	{
		Ready,
		Fighting,
		Results
	}

	public const float MaximumFightHp = 100.0f;

	[Export]
	public NodePath Creature1Path { get; set; } = null!;

	[Export]
	public NodePath Creature2Path { get; set; } = null!;

	[Export]
	public float BaseDamage { get; set; } = 5.0f;

	[Export]
	public float PowerDamageMultiplier { get; set; } = 0.15f;

	[Export(PropertyHint.Range, "0,0.3,0.01")]
	public float DamageVariance { get; set; } = 0.10f;

	[Export]
	public float InitialAttackDelay { get; set; } = 1.0f;

	[Export]
	public float AttackInterval { get; set; } = 1.15f;

	[Export]
	public float ResultDisplayDuration { get; set; } = 2.5f;

	private readonly RandomNumberGenerator _random = new();
	private Creature _creature1 = null!;
	private Creature _creature2 = null!;
	private Marker2D _creature1Position = null!;
	private Marker2D _creature2Position = null!;
	private Label _fightDisplay = null!;
	private FightState _state = FightState.Ready;
	private float _creature1Hp;
	private float _creature2Hp;
	private float _timeUntilAttack;
	private float _resultTimeRemaining;
	private bool _creature1AttacksNext;
	private string _lastAttackText = string.Empty;

	public override void _Ready()
	{
		_creature1 = GetNode<Creature>(Creature1Path);
		_creature2 = GetNode<Creature>(Creature2Path);
		_creature1Position = GetNode<Marker2D>("Creature1Position");
		_creature2Position = GetNode<Marker2D>("Creature2Position");
		_fightDisplay = GetNode<Label>("FightUI/FightDisplay");
		_random.Randomize();
	}

	public override void _Process(double delta)
	{
		if (_state == FightState.Fighting)
			UpdateFight((float)delta);
		else if (_state == FightState.Results)
			UpdateResults((float)delta);
	}

	public bool TryInteract(Node interactor)
	{
		if (_state != FightState.Ready
			|| interactor is not Player
			|| !_creature1.CanBeginCompetition
			|| !_creature2.CanBeginCompetition)
			return false;

		BeginFight();
		return true;
	}

	private void BeginFight()
	{
		_creature1Hp = MaximumFightHp;
		_creature2Hp = MaximumFightHp;
		_timeUntilAttack = InitialAttackDelay;
		_creature1AttacksNext = _random.RandiRange(0, 1) == 0;
		_lastAttackText = "Fight!";
		_state = FightState.Fighting;

		_creature1.PrepareForFight(_creature1Position.GlobalPosition, _creature2Position.GlobalPosition);
		_creature2.PrepareForFight(_creature2Position.GlobalPosition, _creature1Position.GlobalPosition);
		_fightDisplay.Visible = true;
		UpdateFightDisplay();
	}

	private void UpdateFight(float delta)
	{
		_timeUntilAttack -= delta;
		if (_timeUntilAttack > 0.0f)
			return;

		_timeUntilAttack += AttackInterval;
		if (_creature1AttacksNext)
			PerformAttack(_creature1, _creature2, true);
		else
			PerformAttack(_creature2, _creature1, false);

		_creature1AttacksNext = !_creature1AttacksNext;
	}

	private void PerformAttack(Creature attacker, Creature receiver, bool creature1IsAttacker)
	{
		float rawDamage = BaseDamage + attacker.CompetitionPower * PowerDamageMultiplier;
		float varianceMultiplier = _random.RandfRange(1.0f - DamageVariance, 1.0f + DamageVariance);
		float damage = Mathf.Max(1.0f, Mathf.Round(rawDamage * varianceMultiplier));

		if (creature1IsAttacker)
			_creature2Hp = Mathf.Max(0.0f, _creature2Hp - damage);
		else
			_creature1Hp = Mathf.Max(0.0f, _creature1Hp - damage);

		attacker.ShowFightAttack(receiver.GlobalPosition);
		receiver.ShowFightHit();
		_lastAttackText = $"{(creature1IsAttacker ? "Creature 1" : "Creature 2")} hits for {damage:0}!";
		UpdateFightDisplay();

		if (_creature1Hp <= 0.0f || _creature2Hp <= 0.0f)
			FinishFight(_creature1Hp > 0.0f);
	}

	private void FinishFight(bool creature1Wins)
	{
		_state = FightState.Results;
		_resultTimeRemaining = ResultDisplayDuration;
		_creature1.PauseFight(creature1Wins);
		_creature2.PauseFight(!creature1Wins);
		_fightDisplay.Text = $"{(creature1Wins ? "Creature 1" : "Creature 2")} Wins!\nCreature 1 HP: {_creature1Hp:0} / {MaximumFightHp:0}\nCreature 2 HP: {_creature2Hp:0} / {MaximumFightHp:0}";
	}

	private void UpdateResults(float delta)
	{
		_resultTimeRemaining -= delta;
		if (_resultTimeRemaining > 0.0f)
			return;

		_creature1.EndFight();
		_creature2.EndFight();
		_fightDisplay.Visible = false;
		_lastAttackText = string.Empty;
		_creature1Hp = 0.0f;
		_creature2Hp = 0.0f;
		_state = FightState.Ready;
	}

	private void UpdateFightDisplay()
	{
		_fightDisplay.Text = $"Creature 1 HP: {_creature1Hp:0} / {MaximumFightHp:0}\nCreature 2 HP: {_creature2Hp:0} / {MaximumFightHp:0}\n{_lastAttackText}";
	}
}
