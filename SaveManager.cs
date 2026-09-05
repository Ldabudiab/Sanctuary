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

	[Export]
	public NodePath WorldTimePath { get; set; } = null!;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = true
	};

	private Label _feedbackLabel = null!;
	private WorldTime _worldTime = null!;
	private readonly List<Creature> _persistentCreatures = new();
	private float _feedbackTimeRemaining;
	private bool _manualLoadPending;
	private bool _isLoading;

	public override void _Ready()
	{
		_feedbackLabel = GetNode<Label>(FeedbackLabelPath);
		_worldTime = GetNode<WorldTime>(WorldTimePath);
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
		else if (keyEvent.Keycode == Key.F4 || keyEvent.PhysicalKeycode == Key.F4)
		{
			RequestManualLoad();
			GetViewport().SetInputAsHandled();
		}
	}

	private void RequestManualLoad()
	{
		if (_manualLoadPending || _isLoading)
			return;

		_manualLoadPending = true;
		GD.Print("F4 load requested");
		CallDeferred(MethodName.PerformManualLoad);
	}

	private void PerformManualLoad()
	{
		try
		{
			LoadGame();
		}
		finally
		{
			_manualLoadPending = false;
			GD.Print("Manual load finished");
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
		if (_isLoading)
		{
			GD.PushWarning("A load is already in progress; duplicate request ignored.");
			return false;
		}

		if (!FileAccess.FileExists(SavePath))
		{
			GD.Print("No Save Found");
			if (showFeedback)
				ShowFeedback("No Save Found");
			return false;
		}

		try
		{
			_isLoading = true;
			GD.Print("Save file read started");
			string json;
			using (FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read))
			{
				if (file == null)
				{
					ReportFailure($"Unable to open save file: {FileAccess.GetOpenError()}", showFeedback);
					return false;
				}

				json = file.GetAsText();
			}
			GD.Print("Save file read completed");

			SanctuarySaveData saveData =
				JsonSerializer.Deserialize<SanctuarySaveData>(json, JsonOptions);
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
			GD.PrintErr(exception.ToString());
			return false;
		}
		catch (Exception exception)
		{
			ReportFailure($"Unable to load game: {exception.Message}", showFeedback);
			GD.PrintErr(exception.ToString());
			return false;
		}
		finally
		{
			_isLoading = false;
		}
	}

	private SanctuarySaveData BuildSaveData()
	{
		SanctuarySaveData saveData = new()
		{
			Version = CurrentSaveVersion,
			WorldTime = _worldTime.CreateSaveData()
		};
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
		GD.Print("World time apply started");
		if (saveData.WorldTime != null)
			_worldTime.RestoreSavedState(saveData.WorldTime);
		GD.Print("World time apply completed");

		GD.Print("Creature data apply started");
		foreach (Creature creature in GetPersistentCreatures())
		{
			if (!string.IsNullOrWhiteSpace(creature.PersistentId)
				&& saveData.Creatures.TryGetValue(creature.PersistentId, out CreatureSaveData creatureData)
				&& creatureData != null)
			{
				creature.ApplySaveData(creatureData);
			}
		}
		GD.Print("Creature data apply completed");
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
