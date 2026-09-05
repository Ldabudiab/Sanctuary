using Godot;

public partial class CreatureNeedsDebug : Label
{
	[Export]
	public string CreatureLabel { get; set; } = "Creature";

	[Export]
	public NodePath CreaturePath { get; set; } = null!;

	[Export]
	public NodePath StatsPath { get; set; } = null!;

	[Export]
	public NodePath DevelopmentPath { get; set; } = null!;

	private Creature _creature = null!;
	private CreatureStats _stats = null!;
	private CreatureDevelopment _development = null!;

	public override void _Ready()
	{
		_creature = GetNode<Creature>(CreaturePath);
		_stats = GetNode<CreatureStats>(StatsPath);
		_development = GetNode<CreatureDevelopment>(DevelopmentPath);
	}

	public override void _Process(double delta)
	{
		Text = $"{CreatureLabel}\nState: {_creature.CurrentAiState}\nAge: {_creature.Age}\nStar: {_development.Star:0}\nNatural: {_development.Natural:0}\nVoid: {_development.Void:0}\nSpeed: {_stats.Speed:0}\nPower: {_stats.Power:0}\nEndurance: {_stats.Endurance:0}\nSwimming: {_stats.Swimming:0}\nIntelligence: {_stats.Intelligence:0}";
	}
}
