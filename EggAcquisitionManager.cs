using Godot;
using System;
using System.Collections.Generic;

public partial class EggAcquisitionManager : Node
{
	private const int FirstAcquiredPucaNumber = 4;

	[Export]
	public NodePath WorldTimePath { get; set; } = null!;

	[Export]
	public PackedScene CreatureScene { get; set; } = null!;

	[Export]
	public NodePath SaveManagerPath { get; set; } = null!;

	private WorldTime _worldTime = null!;
	private SaveManager _saveManager = null!;
	private readonly List<EggNest> _nests = new();
	private int _nextPucaNumber = FirstAcquiredPucaNumber;

	public override void _Ready()
	{
		_worldTime = GetNode<WorldTime>(WorldTimePath);
		_saveManager = GetNode<SaveManager>(SaveManagerPath);
		foreach (Node node in GetTree().GetNodesInGroup("egg_nests"))
		{
			if (node is EggNest nest)
				_nests.Add(nest);
		}
	}

	public override void _Process(double delta)
	{
		double now = _worldTime.TotalElapsedGameTime;
		double incubationDuration = _worldTime.FullCycleDuration;
		if (incubationDuration <= 0.0)
			return;

		foreach (EggNest nest in GetNests())
		{
			if (nest.HasEgg && now - nest.IncubationStartedAt >= incubationDuration)
				Hatch(nest);
		}
	}

	public bool TryPlaceEgg(EggNest nest, Player player)
	{
		if (!player.Inventory.Remove(ItemCatalog.PucaEgg))
		{
			nest.ShowMessage("You don't have a Puca Egg.");
			return false;
		}

		nest.BeginIncubation(_worldTime.TotalElapsedGameTime);
		nest.ShowMessage("Puca Egg placed.");
		return true;
	}

	public EggAcquisitionSaveData CreateSaveData()
	{
		EggAcquisitionSaveData data = new()
		{
			NextPucaNumber = _nextPucaNumber
		};
		foreach (EggNest nest in GetNests())
			data.Nests[nest.PersistentId] = nest.CreateSaveData();
		return data;
	}

	public void PrepareLoadedPucas(SanctuarySaveData saveData)
	{
		HashSet<string> savedAcquiredIds = new();
		foreach ((string persistentId, CreatureSaveData creatureData) in saveData.Creatures)
		{
			if (creatureData?.IsAcquired == true)
				savedAcquiredIds.Add(persistentId);
		}

		List<Creature> currentAcquired = new();
		foreach (Node node in GetTree().GetNodesInGroup("creatures"))
		{
			if (node is Creature creature && creature.IsAcquiredPuca)
				currentAcquired.Add(creature);
		}

		foreach (Creature creature in currentAcquired)
		{
			if (!savedAcquiredIds.Contains(creature.PersistentId))
			{
				_saveManager.UnregisterCreature(creature);
				creature.Free();
			}
		}

		HashSet<string> currentIds = GetCurrentPersistentIds();
		foreach ((string persistentId, CreatureSaveData creatureData) in saveData.Creatures)
		{
			if (creatureData?.IsAcquired != true || currentIds.Contains(persistentId))
				continue;

			SpawnPuca(
				persistentId,
				new Vector2(creatureData.PositionX, creatureData.PositionY));
			currentIds.Add(persistentId);
		}

		_nextPucaNumber = Math.Max(
			FirstAcquiredPucaNumber,
			saveData.EggAcquisition?.NextPucaNumber ?? FirstAcquiredPucaNumber);
		while (currentIds.Contains(BuildPersistentId(_nextPucaNumber)))
			_nextPucaNumber++;
	}

	public void RestoreNests(EggAcquisitionSaveData data)
	{
		foreach (EggNest nest in GetNests())
		{
			EggNestSaveData nestData = null;
			data?.Nests?.TryGetValue(nest.PersistentId, out nestData);
			nest.Restore(nestData ?? new EggNestSaveData());
		}
	}

	private void Hatch(EggNest nest)
	{
		string persistentId = AllocatePersistentId();
		nest.ClearEgg();
		nest.ShowMessage("A new Puca hatched!");
		SpawnPuca(persistentId, nest.GlobalPosition + new Vector2(0.0f, 48.0f));
		GD.Print($"Puca Egg hatched as '{persistentId}'.");
	}

	private Creature SpawnPuca(string persistentId, Vector2 position)
	{
		Creature creature = CreatureScene.Instantiate<Creature>();
		creature.Name = $"AcquiredPuca_{persistentId}";
		creature.PersistentId = persistentId;
		creature.IsAcquiredPuca = true;
		creature.Position = position;
		GetParent().AddChild(creature);
		_saveManager.RegisterCreature(creature);
		return creature;
	}

	private string AllocatePersistentId()
	{
		HashSet<string> currentIds = GetCurrentPersistentIds();
		while (currentIds.Contains(BuildPersistentId(_nextPucaNumber)))
			_nextPucaNumber++;

		return BuildPersistentId(_nextPucaNumber++);
	}

	private HashSet<string> GetCurrentPersistentIds()
	{
		HashSet<string> ids = new();
		foreach (Node node in GetTree().GetNodesInGroup("creatures"))
		{
			if (node is Creature creature && !string.IsNullOrWhiteSpace(creature.PersistentId))
				ids.Add(creature.PersistentId);
		}
		return ids;
	}

	private IEnumerable<EggNest> GetNests()
	{
		return _nests;
	}

	private static string BuildPersistentId(int number) => $"creature_{number}";
}
