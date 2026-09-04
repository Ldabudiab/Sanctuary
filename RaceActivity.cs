using Godot;

public partial class RaceActivity : Area2D, IInteractable
{
	private enum RaceState
	{
		Ready,
		Countdown,
		Racing,
		Results
	}

	[Export]
	public NodePath Creature1Path { get; set; } = null!;

	[Export]
	public NodePath Creature2Path { get; set; } = null!;

	[Export]
	public float BaseRaceSpeed { get; set; } = 110.0f;

	[Export(PropertyHint.Range, "0,0.2,0.01")]
	public float SpeedVariance { get; set; } = 0.05f;

	[Export]
	public float CountdownStepDuration { get; set; } = 1.0f;

	[Export]
	public float GoDisplayDuration { get; set; } = 0.65f;

	[Export]
	public float ResultDisplayDuration { get; set; } = 2.5f;

	private readonly RandomNumberGenerator _random = new();
	private Creature _creature1 = null!;
	private Creature _creature2 = null!;
	private Marker2D _lane1Start = null!;
	private Marker2D _lane1Finish = null!;
	private Marker2D _lane2Start = null!;
	private Marker2D _lane2Finish = null!;
	private Label _message = null!;
	private RaceState _state = RaceState.Ready;
	private float _stateTimeRemaining;
	private float _creature1RaceSpeed;
	private float _creature2RaceSpeed;
	private int _countdownNumber;

	public override void _Ready()
	{
		_creature1 = GetNode<Creature>(Creature1Path);
		_creature2 = GetNode<Creature>(Creature2Path);
		_lane1Start = GetNode<Marker2D>("Lane1Start");
		_lane1Finish = GetNode<Marker2D>("Lane1Finish");
		_lane2Start = GetNode<Marker2D>("Lane2Start");
		_lane2Finish = GetNode<Marker2D>("Lane2Finish");
		_message = GetNode<Label>("RaceUI/Message");
		_random.Randomize();
	}

	public override void _Process(double delta)
	{
		float frameDelta = (float)delta;

		switch (_state)
		{
			case RaceState.Countdown:
				UpdateCountdown(frameDelta);
				break;
			case RaceState.Racing:
				UpdateRace();
				break;
			case RaceState.Results:
				UpdateResults(frameDelta);
				break;
		}
	}

	public bool TryInteract(Node interactor)
	{
		if (_state != RaceState.Ready || interactor is not Player)
			return false;

		BeginRace();
		return true;
	}

	private void BeginRace()
	{
		_creature1RaceSpeed = CalculateRaceSpeed(_creature1.CompetitionSpeed);
		_creature2RaceSpeed = CalculateRaceSpeed(_creature2.CompetitionSpeed);

		_creature1.PrepareForRace(_lane1Start.GlobalPosition, _lane1Finish.GlobalPosition, _creature1RaceSpeed);
		_creature2.PrepareForRace(_lane2Start.GlobalPosition, _lane2Finish.GlobalPosition, _creature2RaceSpeed);

		_state = RaceState.Countdown;
		_countdownNumber = 3;
		_stateTimeRemaining = CountdownStepDuration;
		UpdateCountdownMessage("3");
	}

	private float CalculateRaceSpeed(float speedStat)
	{
		float statMultiplier = 1.0f + speedStat / CreatureStats.MaximumStatValue;
		float temporaryVariance = _random.RandfRange(1.0f - SpeedVariance, 1.0f + SpeedVariance);
		return BaseRaceSpeed * statMultiplier * temporaryVariance;
	}

	private void UpdateCountdown(float delta)
	{
		_stateTimeRemaining -= delta;
		if (_stateTimeRemaining > 0.0f)
			return;

		if (_countdownNumber > 1)
		{
			_countdownNumber--;
			_stateTimeRemaining = CountdownStepDuration;
			UpdateCountdownMessage(_countdownNumber.ToString());
			return;
		}

		if (_countdownNumber == 1)
		{
			_countdownNumber = 0;
			_stateTimeRemaining = GoDisplayDuration;
			UpdateCountdownMessage("GO!");
			_creature1.StartRaceMovement();
			_creature2.StartRaceMovement();
			return;
		}

		_state = RaceState.Racing;
		_message.Text = RaceSpeedText("Racing");
	}

	private void UpdateRace()
	{
		bool creature1Finished = _creature1.HasReachedRaceFinish();
		bool creature2Finished = _creature2.HasReachedRaceFinish();

		if (!creature1Finished && !creature2Finished)
			return;

		Creature winner;
		string winnerName;
		if (creature1Finished && creature2Finished)
		{
			float creature1Distance = _creature1.GlobalPosition.DistanceSquaredTo(_lane1Finish.GlobalPosition);
			float creature2Distance = _creature2.GlobalPosition.DistanceSquaredTo(_lane2Finish.GlobalPosition);
			bool creature1Wins = creature1Distance <= creature2Distance;
			winner = creature1Wins ? _creature1 : _creature2;
			winnerName = creature1Wins ? "Creature 1" : "Creature 2";
		}
		else
		{
			winner = creature1Finished ? _creature1 : _creature2;
			winnerName = creature1Finished ? "Creature 1" : "Creature 2";
		}

		FinishRace(winner, winnerName);
	}

	private void FinishRace(Creature winner, string winnerName)
	{
		_state = RaceState.Results;
		_stateTimeRemaining = ResultDisplayDuration;
		_creature1.PauseRaceAtFinish(_creature1 == winner);
		_creature2.PauseRaceAtFinish(_creature2 == winner);
		_message.Text = $"{winnerName} Wins!";
	}

	private void UpdateResults(float delta)
	{
		_stateTimeRemaining -= delta;
		if (_stateTimeRemaining > 0.0f)
			return;

		_creature1.EndRace();
		_creature2.EndRace();
		_message.Visible = false;
		_state = RaceState.Ready;
	}

	private void UpdateCountdownMessage(string countdownText)
	{
		_message.Visible = true;
		_message.Text = RaceSpeedText(countdownText);
	}

	private string RaceSpeedText(string heading)
	{
		return $"{heading}\nCreature 1: {_creature1RaceSpeed:0.0}\nCreature 2: {_creature2RaceSpeed:0.0}";
	}
}
