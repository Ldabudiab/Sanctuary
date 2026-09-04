using Godot;

public partial class DoorwayTrigger : Area2D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string DestinationScenePath { get; set; } = string.Empty;

	[Export]
	public string DestinationSpawnPoint { get; set; } = string.Empty;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player)
			WorldTransition.TryTravel(this, DestinationScenePath, DestinationSpawnPoint);
	}
}
