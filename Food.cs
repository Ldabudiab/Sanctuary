using Godot;

public partial class Food : Area2D, IInteractable
{
	public bool TryInteract(Node interactor)
	{
		if (interactor is not Player player || !player.TryPickupItem(CarriedItem.CreateFood()))
			return false;

		QueueFree();
		return true;
	}
}
