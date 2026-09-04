using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveManager : Node
{
	public const int CurrentSaveVersion = 1;
	public const string SavePath = "user://sanctuary_save.json";

	[Export]
	public NodePath FeedbackLabelPath { get; set; } = null!;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = true
	};

	private Label _feedbackLabel = null!;
	private readonly List<Creature> _persistentCreatures = new();
	private float _feedbackTimeRemaining;

	public override void _Ready()
	{
		_feedbackLabel = GetNode<Label>(FeedbackLabelPath);
		foreach (Node node in GetTree().GetNodesInGroup("creatures"))
		{
			if (node is Creature creature)
				_persistentCreatures.Add(creature);
		}
	}

	public override void _Process(double delta)
	{
		if (_feedbackTimeRemaining <= 0.0f)
			return;

		_feedbackTimeRemaining -= (float)delta;
		if (_feedbackTimeRemaining <= 0.0f)
			_feedbackLabel.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
			return;

		if (keyEvent.Keycode == Key.F5 || keyEvent.PhysicalKeycode == Key.F5)
		{
			SaveGame();
			GetViewport().SetInputAsHandled();
		}
		else if (keyEvent.Keycode == Key.F9 || keyEvent.PhysicalKeycode == Key.F9)
		{
			LoadGame();
			GetViewport().SetInputAsHandled();
		}
	}

	public bool SaveGame(bool showFeedback = true)
	{
		try
		{
			SanctuarySaveData saveData = BuildSaveData();
			string json = JsonSerializer.Serialize(saveData, JsonOptions);
			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				ReportFailure($"Unable to open save file for writing: {FileAccess.GetOpenError()}", showFeedback);
				return false;
			}

			file.StoreString(json);
			GD.Print($"Game Saved: {ProjectSettings.GlobalizePath(SavePath)}");
			if (showFeedback)
				ShowFeedback("Game Saved");
			return true;
		}
		catch (Exception exception)
		{
			ReportFailure($"Unable to save game: {exception.Message}", showFeedback);
			return false;
		}
	}

	public bool LoadGame(bool showFeedback = true)
	{
		if (!FileAccess.FileExists(SavePath))
		{
			GD.Print("No Save Found");
			if (showFeedback)
				ShowFeedback("No Save Found");
			return false;
		}

		try
		{
			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				ReportFailure($"Unable to open save file: {FileAccess.GetOpenError()}", showFeedback);
				return false;
			}

			SanctuarySaveData saveData =
				JsonSerializer.Deserialize<SanctuarySaveData>(file.GetAsText(), JsonOptions);
			if (saveData == null || saveData.Creatures == null)
			{
				ReportFailure("Save data is empty or malformed.", showFeedback);
				return false;
			}

			if (saveData.Version != CurrentSaveVersion)
			{
				ReportFailure(
					$"Unsupported save version {saveData.Version}; expected {CurrentSaveVersion}.",
					showFeedback);
				return false;
			}

			ApplySaveData(saveData);
			GD.Print("Game Loaded");
			if (showFeedback)
				ShowFeedback("Game Loaded");
			return true;
		}
		catch (JsonException exception)
		{
			ReportFailure($"Save data is corrupt: {exception.Message}", showFeedback);
			return false;
		}
		catch (Exception exception)
		{
			ReportFailure($"Unable to load game: {exception.Message}", showFeedback);
			return false;
		}
	}

	private SanctuarySaveData BuildSaveData()
	{
		SanctuarySaveData saveData = new() { Version = CurrentSaveVersion };
		foreach (Creature creature in GetPersistentCreatures())
		{
			if (string.IsNullOrWhiteSpace(creature.PersistentId))
			{
				GD.PushWarning($"Skipping creature '{creature.Name}' because it has no PersistentId.");
				continue;
			}

			if (!saveData.Creatures.TryAdd(creature.PersistentId, creature.CreateSaveData()))
				GD.PushWarning($"Duplicate creature PersistentId '{creature.PersistentId}' was skipped.");
		}
		return saveData;
	}

	private void ApplySaveData(SanctuarySaveData saveData)
	{
		foreach (Creature creature in GetPersistentCreatures())
		{
			if (!string.IsNullOrWhiteSpace(creature.PersistentId)
				&& saveData.Creatures.TryGetValue(creature.PersistentId, out CreatureSaveData creatureData)
				&& creatureData != null)
			{
				creature.ApplySaveData(creatureData);
			}
		}
	}

	private IEnumerable<Creature> GetPersistentCreatures()
	{
		return _persistentCreatures;
	}

	private void ReportFailure(string message, bool showFeedback)
	{
		GD.PushWarning(message);
		if (showFeedback)
			ShowFeedback(message.StartsWith("Unsupported") ? "Unsupported Save Version" : "Save Error");
	}

	private void ShowFeedback(string message)
	{
		_feedbackLabel.Text = message;
		_feedbackLabel.Visible = true;
		_feedbackTimeRemaining = 2.0f;
	}
}
