using Godot;

public partial class StatsWindowToggle : CanvasLayer
{
	[Export]
	public NodePath StatsWindowPath { get; set; } = null!;

	private CanvasLayer _statsWindow = null!;
	private Button _statsButton = null!;

	public override void _Ready()
	{
		_statsWindow = GetNode<CanvasLayer>(StatsWindowPath);
		_statsButton = GetNode<Button>("StatsButton");
		_statsButton.Pressed += OnStatsButtonPressed;
		SetStatsVisible(false);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent
			|| !keyEvent.Pressed
			|| keyEvent.Echo
			|| (keyEvent.Keycode != Key.Period && keyEvent.PhysicalKeycode != Key.Period))
		{
			return;
		}

		Control focusOwner = GetViewport().GuiGetFocusOwner();
		if (focusOwner is LineEdit or TextEdit)
			return;

		SetStatsVisible(!_statsWindow.Visible);
		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_statsButton))
			_statsButton.Pressed -= OnStatsButtonPressed;
	}

	private void OnStatsButtonPressed()
	{
		SetStatsVisible(_statsButton.ButtonPressed);
	}

	private void SetStatsVisible(bool isVisible)
	{
		_statsWindow.Visible = isVisible;
		_statsButton.SetPressedNoSignal(isVisible);
	}
}
