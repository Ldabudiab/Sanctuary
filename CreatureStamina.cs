using Godot;

public partial class CreatureStamina : Node
{
	public const float BaseMaximumStamina = 100.0f;
	public const float StaminaPerEndurancePoint = 2.0f;

	[Export]
	public float CompetitionRestRecoveryPerSecond { get; set; } = 40.0f;

	public float Current { get; private set; }
	public float Maximum => BaseMaximumStamina
		+ _stats.GetValue(CreatureStatType.Endurance) * StaminaPerEndurancePoint;
	public bool IsExhausted => Current <= 0.0f;
	public bool IsFull => Current >= Maximum - 0.01f;

	private CreatureStats _stats = null!;
	private Node2D _display = null!;
	private ProgressBar _bar = null!;
	private Label _valueLabel = null!;
	private bool _activityVisible;

	public override void _Ready()
	{
		_stats = GetNode<CreatureStats>("../Stats");
		_display = GetNode<Node2D>("../StaminaDisplay");
		_bar = GetNode<ProgressBar>("../StaminaDisplay/Bar");
		_valueLabel = GetNode<Label>("../StaminaDisplay/Value");
		Current = Maximum;
		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		Current = Mathf.Min(Current, Maximum);
		UpdateDisplay();
	}

	public void Spend(float amount)
	{
		if (amount <= 0.0f)
			return;

		Current = Mathf.Clamp(Current - amount, 0.0f, Maximum);
	}

	public void RestoreDuringCompetitionRest(float delta)
	{
		Current = Mathf.Clamp(Current + CompetitionRestRecoveryPerSecond * delta, 0.0f, Maximum);
	}

	public void BeginCompetition()
	{
		Current = Maximum;
		_activityVisible = true;
		UpdateDisplay();
	}

	public void EndCompetition()
	{
		Current = Maximum;
		_activityVisible = false;
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		if (_bar == null)
			return;

		_bar.MaxValue = Maximum;
		_bar.Value = Current;
		_valueLabel.Text = $"{Current:0} / {Maximum:0}";
		_display.Visible = _activityVisible;
	}
}
