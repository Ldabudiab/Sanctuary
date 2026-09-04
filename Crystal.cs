using Godot;

public partial class Crystal : Area2D, IInteractable
{
	[Export]
	public CreatureStatType StatType { get; set; } = CreatureStatType.Speed;

	[Export(PropertyHint.Range, "0,100,1")]
	public float StatIncrease { get; set; } = 5.0f;

	public bool TryInteract(Node interactor)
	{
		if (interactor is not Player player
			|| !player.TryPickupItem(CarriedItem.CreateCrystal(StatType, StatIncrease)))
			return false;

		QueueFree();
		return true;
	}
}
