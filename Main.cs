using Godot;

public partial class Main : Node2D
{
	[Export(PropertyHint.Range, "1,100,1")]
	public int DebugAgeIncrease { get; set; } = 25;

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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
			return;

		if (keyEvent.Keycode == Key.F2 || keyEvent.PhysicalKeycode == Key.F2)
		{
			foreach (Node node in GetTree().GetNodesInGroup("creatures"))
			{
				if (node is Creature creature)
					creature.IncreaseAge(DebugAgeIncrease);
			}

			GD.Print($"Debug Age increase: +{DebugAgeIncrease} to each Sanctuary creature.");
			GetViewport().SetInputAsHandled();
		}
		else if (keyEvent.Keycode == Key.F3 || keyEvent.PhysicalKeycode == Key.F3)
		{
			GetNode<Creature>("Creature3").PrepareVoidEvolutionDebugTest();
			GetViewport().SetInputAsHandled();
		}
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
