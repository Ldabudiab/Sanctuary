using Godot;

public partial class EvolutionFruit : Area2D, IInteractable
{
	[Export]
	public CreatureDevelopmentType DevelopmentType { get; set; }

	[Export(PropertyHint.Range, "0,100,1")]
	public float DevelopmentIncrease { get; set; } = 10.0f;

	[Export(PropertyHint.Range, "0,100,1")]
	public int AgeIncrease { get; set; } = 1;

	[Export(PropertyHint.Range, "0,30,0.5")]
	public float RespawnDelay { get; set; } = 5.0f;

	private Polygon2D _fruit = null!;
	private Polygon2D _accent = null!;
	private CollisionShape2D _collisionShape = null!;

	public override void _Ready()
	{
		_fruit = GetNode<Polygon2D>("Fruit");
		_accent = GetNode<Polygon2D>("Accent");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		ApplyAppearance();
	}

	public bool TryInteract(Node interactor)
	{
		if (interactor is not Player player
			|| !Visible
			|| !player.TryPickupItem(CarriedItem.CreateEvolutionFruit(
				DevelopmentType,
				DevelopmentIncrease,
				AgeIncrease)))
		{
			return false;
		}

		Visible = false;
		_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		RespawnAfterDelay();
		return true;
	}

	private async void RespawnAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(RespawnDelay), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree())
			return;

		Visible = true;
		_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
	}

	private void ApplyAppearance()
	{
		(_fruit.Color, _accent.Color) = DevelopmentType switch
		{
			CreatureDevelopmentType.Star => (new Color(0.98f, 0.75f, 0.2f), new Color(1.0f, 0.96f, 0.58f)),
			CreatureDevelopmentType.Natural => (new Color(0.3f, 0.75f, 0.3f), new Color(0.72f, 0.95f, 0.42f)),
			CreatureDevelopmentType.Void => (new Color(0.38f, 0.2f, 0.62f), new Color(0.82f, 0.45f, 0.96f)),
			_ => (Colors.White, Colors.White)
		};
	}
}
