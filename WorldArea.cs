using Godot;

public partial class WorldArea : Node2D
{
	public override void _Ready()
	{
		WorldTransition.PlacePlayerAtPendingSpawn(this);
	}
}
