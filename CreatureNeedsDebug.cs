using Godot;

public partial class CreatureNeedsDebug : Label
{
	[Export]
	public string CreatureLabel { get; set; } = "Creature";

	[Export]
	public NodePath CreaturePath { get; set; } = null!;

	[Export]
	public NodePath StatsPath { get; set; } = null!;

	private Creature _creature = null!;
	private CreatureStats _stats = null!;

	public override void _Ready()
	{
		_creature = GetNode<Creature>(CreaturePath);
		_stats = GetNode<CreatureStats>(StatsPath);
	}

	public override void _Process(double delta)
	{
		Text = $"{CreatureLabel}\nState: {_creature.CurrentAiState}\nSpeed: {_stats.Speed:0}\nPower: {_stats.Power:0}\nEndurance: {_stats.Endurance:0}\nSwimming: {_stats.Swimming:0}\nIntelligence: {_stats.Intelligence:0}";
	}
}
