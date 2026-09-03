using Godot;

public partial class CreatureNeedsDebug : Label
{
	[Export]
	public NodePath NeedsPath { get; set; } = null!;

	[Export]
	public NodePath PersonalityPath { get; set; } = null!;

	private CreatureNeeds _needs = null!;
	private CreaturePersonality _personality = null!;

	public override void _Ready()
	{
		_needs = GetNode<CreatureNeeds>(NeedsPath);
		_personality = GetNode<CreaturePersonality>(PersonalityPath);
	}

	public override void _Process(double delta)
	{
		Text = $"Hunger: {_needs.Hunger:0}\nHappiness: {_needs.Happiness:0}\nEnergy: {_personality.Energy:0}\nAttachment: {_personality.Attachment:0}";
	}
}
