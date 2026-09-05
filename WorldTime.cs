using Godot;
using System;

public partial class WorldTime : Node
{
	[Export(PropertyHint.Range, "1,3600,1")]
	public double DayDuration { get; set; } = 180.0;

	[Export(PropertyHint.Range, "1,3600,1")]
	public double NightDuration { get; set; } = 60.0;

	[Export(PropertyHint.Range, "0,120,1")]
	public double EveningTransitionDuration { get; set; } = 30.0;

	[Export(PropertyHint.Range, "0,120,1")]
	public double DawnTransitionDuration { get; set; } = 15.0;

	[Export(PropertyHint.Range, "0,100,1")]
	public int AgePerDay { get; set; } = 10;

	[Export]
	public Color NightColor { get; set; } = new(0.46f, 0.56f, 0.78f, 1.0f);

	[Export]
	public NodePath CanvasModulatePath { get; set; } = null!;

	[Export]
	public NodePath DebugLabelPath { get; set; } = null!;

	public int CurrentDay { get; private set; } = 1;
	public double TimeOfDay { get; private set; }
	public double FullCycleDuration => DayDuration + NightDuration;
	public double NormalizedDayProgress => FullCycleDuration <= 0.0 ? 0.0 : TimeOfDay / FullCycleDuration;
	public bool IsDay => TimeOfDay < DayDuration;

	public event Action<int> NewDayStarted = delegate { };

	private CanvasModulate _canvasModulate = null!;
	private Label _debugLabel = null!;

	public override void _Ready()
	{
		_canvasModulate = GetNode<CanvasModulate>(CanvasModulatePath);
		_debugLabel = GetNode<Label>(DebugLabelPath);
		RefreshPresentation();
	}

	public override void _Process(double delta)
	{
		AdvanceTime(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
			return;

		if (keyEvent.Keycode == Key.F6 || keyEvent.PhysicalKeycode == Key.F6)
		{
			AdvanceTime(30.0);
			GetViewport().SetInputAsHandled();
		}
		else if (keyEvent.Keycode == Key.F7 || keyEvent.PhysicalKeycode == Key.F7)
		{
			AdvanceToNextDay();
			GetViewport().SetInputAsHandled();
		}
	}

	public void AdvanceTime(double seconds)
	{
		if (seconds <= 0.0 || FullCycleDuration <= 0.0)
			return;

		double accumulatedTime = TimeOfDay + seconds;
		int completedCycles = (int)Math.Floor(accumulatedTime / FullCycleDuration);
		TimeOfDay = accumulatedTime % FullCycleDuration;

		for (int cycle = 0; cycle < completedCycles; cycle++)
		{
			CurrentDay++;
			NewDayStarted.Invoke(CurrentDay);
		}

		RefreshPresentation();
	}

	public void AdvanceToNextDay()
	{
		AdvanceTime(FullCycleDuration - TimeOfDay);
	}

	public WorldTimeSaveData CreateSaveData()
	{
		return new WorldTimeSaveData
		{
			CurrentDay = CurrentDay,
			TimeOfDay = TimeOfDay
		};
	}

	public void RestoreSavedState(WorldTimeSaveData saveData)
	{
		// Restoration deliberately bypasses AdvanceTime so it cannot emit NewDayStarted.
		CurrentDay = Math.Max(1, saveData.CurrentDay);
		TimeOfDay = FullCycleDuration <= 0.0
			? 0.0
			: Math.Clamp(saveData.TimeOfDay, 0.0, Math.Max(0.0, FullCycleDuration - 0.001));
		RefreshPresentation();
	}

	private void RefreshPresentation()
	{
		if (IsInstanceValid(_canvasModulate))
			_canvasModulate.Color = CalculateWorldColor();

		if (IsInstanceValid(_debugLabel))
		{
			double phaseElapsed = IsDay ? TimeOfDay : TimeOfDay - DayDuration;
			double phaseDuration = IsDay ? DayDuration : NightDuration;
			_debugLabel.Text = $"Day {CurrentDay}  {(IsDay ? "Day" : "Night")}  {FormatTime(phaseElapsed)} / {FormatTime(phaseDuration)}\nF4: Load  F5: Save  F6: +30s  F7: Next day";
		}
	}

	private Color CalculateWorldColor()
	{
		double eveningStart = Math.Max(0.0, DayDuration - EveningTransitionDuration);
		double dawnStart = Math.Max(DayDuration, FullCycleDuration - DawnTransitionDuration);

		if (TimeOfDay < eveningStart)
			return Colors.White;

		if (TimeOfDay < DayDuration && EveningTransitionDuration > 0.0)
		{
			float blend = SmoothStep((float)((TimeOfDay - eveningStart) / EveningTransitionDuration));
			return Colors.White.Lerp(NightColor, blend);
		}

		if (TimeOfDay >= dawnStart && DawnTransitionDuration > 0.0)
		{
			float blend = SmoothStep((float)((TimeOfDay - dawnStart) / DawnTransitionDuration));
			return NightColor.Lerp(Colors.White, blend);
		}

		return NightColor;
	}

	private static float SmoothStep(float value)
	{
		value = Mathf.Clamp(value, 0.0f, 1.0f);
		return value * value * (3.0f - (2.0f * value));
	}

	private static string FormatTime(double seconds)
	{
		int totalSeconds = Math.Max(0, (int)Math.Floor(seconds));
		return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
	}
}
