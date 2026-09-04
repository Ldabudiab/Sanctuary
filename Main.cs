using Godot;

public partial class Main : Node2D
{
	private SaveManager _saveManager = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		WorldTransition.PlacePlayerAtPendingSpawn(this);
		_saveManager = GetNode<SaveManager>("SaveManager");
		_saveManager.LoadGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_saveManager))
			_saveManager.SaveGame(false);
	}
}
