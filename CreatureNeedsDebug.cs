using Godot;

public partial class CreatureNeedsDebug : Label
{
	[Export]
	public NodePath NeedsPath { get; set; } = null!;

	private CreatureNeeds _needs = null!;

	public override void _Ready()
	{
		_needs = GetNode<CreatureNeeds>(NeedsPath);
	}

	public override void _Process(double delta)
	{
		Text = $"Hunger: {_needs.Hunger:0}\nHappiness: {_needs.Happiness:0}";
	}
}
