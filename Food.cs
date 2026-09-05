using Godot;

public partial class Food : Area2D, IInteractable
{
	[Export(PropertyHint.Range, "0,10,1")]
	public float EnduranceIncrease { get; set; } = 1.0f;

	public bool TryInteract(Node interactor)
	{
		if (interactor is not Player player || !player.TryPickupItem(CarriedItem.CreateFood(EnduranceIncrease)))
			return false;

		QueueFree();
		return true;
	}
}
