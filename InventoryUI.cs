using Godot;
using System.Collections.Generic;

public partial class InventoryUI : CanvasLayer
{
	private readonly List<Button> _slotButtons = new();
	private Player _player = null!;
	private Control _window = null!;
	private GridContainer _grid = null!;
	private Label _selection = null!;
	private int _selectedSlot = -1;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_window = GetNode<Control>("Window");
		_grid = GetNode<GridContainer>("Window/Panel/Margin/Layout/Grid");
		_selection = GetNode<Label>("Window/Panel/Margin/Layout/Selection");

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
		_selectedSlot = -1;
		_selection.Text = "Select an inventory slot.";
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
		_selectedSlot = index;
		InventorySlot slot = PlayerInventory.Current.Slots[index];
		_selection.Text = slot.IsEmpty
			? $"Slot {index + 1} is empty."
			: ItemCatalog.Get(slot.ItemId).DisplayName;
		Refresh();
	}

	private void Refresh()
	{
		for (int index = 0; index < _slotButtons.Count; index++)
		{
			InventorySlot slot = PlayerInventory.Current.Slots[index];
			_slotButtons[index].Text = slot.IsEmpty
				? $"Slot {index + 1}\n—"
				: $"{ItemCatalog.Get(slot.ItemId).DisplayName}{(slot.Quantity > 1 ? $"\n×{slot.Quantity}" : string.Empty)}";
			_slotButtons[index].Modulate = index == _selectedSlot
				? new Color(1.0f, 0.84f, 0.42f)
				: Colors.White;
		}
	}
}
