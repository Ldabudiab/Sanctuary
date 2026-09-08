using Godot;
using System.Collections.Generic;

public partial class InventoryUI : CanvasLayer
{
	private readonly List<Button> _slotButtons = new();
	private Player _player = null!;
	private Control _window = null!;
	private GridContainer _grid = null!;
	private Label _selection = null!;
	private Label _pickupFeedback = null!;
	private float _feedbackTimeRemaining;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_window = GetNode<Control>("Window");
		_grid = GetNode<GridContainer>("Window/Panel/Margin/Layout/Grid");
		_selection = GetNode<Label>("Window/Panel/Margin/Layout/Selection");
		_pickupFeedback = GetNode<Label>("PickupFeedback");

		for (int index = 0; index < Inventory.SlotCount; index++)
		{
			int slotIndex = index;
			Button button = new()
			{
				CustomMinimumSize = new Vector2(102.0f, 68.0f),
				FocusMode = Control.FocusModeEnum.None,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand
			};
			button.Pressed += () => SelectSlot(slotIndex);
			_grid.AddChild(button);
			_slotButtons.Add(button);
		}

		PlayerInventory.Current.Changed += Refresh;
		GetNode<Button>("Window/Panel/Margin/Layout/Close").Pressed += Close;
		Refresh();
		_window.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_feedbackTimeRemaining <= 0.0f)
			return;
		_feedbackTimeRemaining -= (float)delta;
		if (_feedbackTimeRemaining <= 0.0f)
			_pickupFeedback.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
			return;

		bool isTab = keyEvent.Keycode == Key.Tab || keyEvent.PhysicalKeycode == Key.Tab;
		bool isEscape = keyEvent.Keycode == Key.Escape || keyEvent.PhysicalKeycode == Key.Escape;
		if (isTab && (_window.Visible || _player.IsGameplayInputEnabled))
		{
			if (_window.Visible)
				Close();
			else
				Open();
			GetViewport().SetInputAsHandled();
		}
		else if (isEscape && _window.Visible)
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _ExitTree()
	{
		PlayerInventory.Current.Changed -= Refresh;
	}

	private void Open()
	{
		UpdateSelectionText();
		Refresh();
		_player.SetGameplayInputEnabled(false);
		_window.Visible = true;
	}

	private void Close()
	{
		_window.Visible = false;
		_player.SetGameplayInputEnabled(true);
	}

	private void SelectSlot(int index)
	{
		PlayerInventory.Current.SelectSlot(index);
		UpdateSelectionText();
	}

	public void ShowFeedback(string message)
	{
		_pickupFeedback.Text = message;
		_pickupFeedback.Visible = true;
		_feedbackTimeRemaining = 2.0f;
	}

	private void Refresh()
	{
		for (int index = 0; index < _slotButtons.Count; index++)
		{
			InventorySlot slot = PlayerInventory.Current.Slots[index];
			_slotButtons[index].Text = slot.IsEmpty
				? $"Slot {index + 1}\n—"
				: $"{ItemCatalog.Get(slot.ItemId).DisplayName}{(slot.Quantity > 1 ? $"\n×{slot.Quantity}" : string.Empty)}";
			_slotButtons[index].Modulate = index == PlayerInventory.Current.SelectedSlotIndex
				? new Color(1.0f, 0.84f, 0.42f)
				: Colors.White;
		}
		UpdateSelectionText();
	}

	private void UpdateSelectionText()
	{
		ItemDefinition selected = PlayerInventory.Current.GetSelectedItem();
		_selection.Text = selected == null
			? "No item selected."
			: $"Selected: {selected.DisplayName}";
	}
}
