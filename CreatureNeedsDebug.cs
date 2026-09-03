using Godot;

public partial class CreatureNeedsDebug : Label
{
	[Export]
	public string CreatureLabel { get; set; } = "Creature";

	[Export]
	public NodePath NeedsPath { get; set; } = null!;

	[Export]
	public NodePath PersonalityPath { get; set; } = null!;

	[Export]
	public NodePath CreaturePath { get; set; } = null!;

	private CreatureNeeds _needs = null!;
	private CreaturePersonality _personality = null!;
	private Creature _creature = null!;

	public override void _Ready()
	{
		_needs = GetNode<CreatureNeeds>(NeedsPath);
		_personality = GetNode<CreaturePersonality>(PersonalityPath);
		_creature = GetNode<Creature>(CreaturePath);
	}

	public override void _Process(double delta)
	{
		Text = $"{CreatureLabel}\nState: {_creature.CurrentAiState}\nHunger: {_needs.Hunger:0}\nHappiness: {_needs.Happiness:0}\nEnergy: {_needs.Energy:0}\nActivity: {_personality.Activity:0}\nAttachment: {_personality.Attachment:0}\nCuriosity: {_personality.Curiosity:0}\nSocial: {_personality.Social:0}\nTemperament: {_personality.Temperament:0}";
	}
}
