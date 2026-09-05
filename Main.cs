using Godot;

public partial class Main : Node2D
{
	private SaveManager _saveManager = null!;
	private WorldTime _worldTime = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		WorldTransition.PlacePlayerAtPendingSpawn(this);
		_worldTime = GetNode<WorldTime>("WorldTime");
		_worldTime.NewDayStarted += OnNewDayStarted;
		_saveManager = GetNode<SaveManager>("SaveManager");
		_saveManager.LoadGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_worldTime))
			_worldTime.NewDayStarted -= OnNewDayStarted;

		if (IsInstanceValid(_saveManager))
			_saveManager.SaveGame(false);
	}

	private void OnNewDayStarted(int currentDay)
	{
		foreach (Node node in GetTree().GetNodesInGroup("creatures"))
		{
			if (node is Creature creature)
				creature.IncreaseAge(_worldTime.AgePerDay);
		}

		GD.Print($"Day {currentDay} began. Sanctuary creature Age increased by {_worldTime.AgePerDay}.");
	}
}
