using System;
using System.Collections.Generic;

public sealed class InventorySlot
{
	public string ItemId { get; private set; } = string.Empty;
	public int Quantity { get; private set; }
	public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

	internal void Set(string itemId, int quantity)
	{
		ItemId = quantity > 0 ? itemId : string.Empty;
		Quantity = Math.Max(0, quantity);
	}
}

public sealed class Inventory
{
	public const int SlotCount = 10;

	private readonly List<InventorySlot> _slots = new(SlotCount);

	public IReadOnlyList<InventorySlot> Slots => _slots;
	public string SelectedItemId { get; private set; } = string.Empty;
	public int SelectedSlotIndex => _slots.FindIndex(slot => !slot.IsEmpty && slot.ItemId == SelectedItemId);
	public event Action Changed;

	public Inventory()
	{
		for (int index = 0; index < SlotCount; index++)
			_slots.Add(new InventorySlot());
	}

	public bool Add(ItemDefinition item, int quantity = 1)
	{
		if (item == null || quantity <= 0)
			return false;

		InventorySlot slot = FindSlot(item.Id) ?? FindEmptySlot();
		if (slot == null)
			return false;

		slot.Set(item.Id, checked(slot.Quantity + quantity));
		Changed?.Invoke();
		return true;
	}

	public bool Remove(ItemDefinition item, int quantity = 1)
	{
		if (item == null || quantity <= 0)
			return false;

		InventorySlot slot = FindSlot(item.Id);
		if (slot == null || slot.Quantity < quantity)
			return false;

		slot.Set(item.Id, slot.Quantity - quantity);
		if (slot.IsEmpty && SelectedItemId == item.Id)
			SelectedItemId = string.Empty;
		Changed?.Invoke();
		return true;
	}

	public bool Contains(ItemDefinition item, int quantity = 1)
	{
		return item != null && quantity > 0 && GetQuantity(item) >= quantity;
	}

	public int GetQuantity(ItemDefinition item)
	{
		return item == null ? 0 : FindSlot(item.Id)?.Quantity ?? 0;
	}

	public bool CanAdd(ItemDefinition item)
	{
		return item != null && (FindSlot(item.Id) != null || FindEmptySlot() != null);
	}

	public void SelectSlot(int index)
	{
		SelectedItemId = index >= 0 && index < _slots.Count && !_slots[index].IsEmpty
			? _slots[index].ItemId
			: string.Empty;
		Changed?.Invoke();
	}

	public ItemDefinition GetSelectedItem()
	{
		return string.IsNullOrEmpty(SelectedItemId) ? null : ItemCatalog.Get(SelectedItemId);
	}

	public InventorySaveData CreateSaveData()
	{
		InventorySaveData data = new();
		foreach (InventorySlot slot in _slots)
		{
			data.Slots.Add(new InventorySlotSaveData
			{
				ItemId = slot.ItemId,
				Quantity = slot.Quantity
			});
		}
		return data;
	}

	public void Restore(InventorySaveData data)
	{
		for (int index = 0; index < SlotCount; index++)
		{
			InventorySlotSaveData savedSlot = data?.Slots != null && index < data.Slots.Count
				? data.Slots[index]
				: null;
			_slots[index].Set(savedSlot?.ItemId ?? string.Empty, savedSlot?.Quantity ?? 0);
		}
		if (GetQuantity(ItemCatalog.Get(SelectedItemId)) <= 0)
			SelectedItemId = string.Empty;
		Changed?.Invoke();
	}

	private InventorySlot FindSlot(string itemId)
	{
		return _slots.Find(slot => !slot.IsEmpty && slot.ItemId == itemId);
	}

	private InventorySlot FindEmptySlot()
	{
		return _slots.Find(slot => slot.IsEmpty);
	}
}

public static class PlayerInventory
{
	public static Inventory Current { get; } = new();
}
