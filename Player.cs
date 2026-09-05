using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MovementSpeed { get; set; } = 200.0f;

	public Vector2 FacingDirection { get; private set; } = Vector2.Down;
	public CarriedItem CarriedItem { get; private set; }
	public bool IsCarryingFood => CarriedItem?.Kind == CarriedItemKind.Food;

	private Node2D _visual = null!;
	private Area2D _interactionArea = null!;
	private CanvasItem _carriedFoodVisual = null!;
	private CanvasItem _carriedCrystalVisual = null!;
	private CanvasItem _carriedEvolutionFruitVisual = null!;
	private Polygon2D _carriedEvolutionFruitShape = null!;
	private Polygon2D _carriedEvolutionFruitAccent = null!;
	private Polygon2D _carriedCrystalShape = null!;
	private Polygon2D _carriedCrystalHighlight = null!;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_carriedFoodVisual = GetNode<CanvasItem>("CarriedFoodVisual");
		_carriedCrystalVisual = GetNode<CanvasItem>("CarriedCrystalVisual");
		_carriedCrystalShape = GetNode<Polygon2D>("CarriedCrystalVisual/Crystal");
		_carriedCrystalHighlight = GetNode<Polygon2D>("CarriedCrystalVisual/Highlight");
		_carriedEvolutionFruitVisual = GetNode<CanvasItem>("CarriedEvolutionFruitVisual");
		_carriedEvolutionFruitShape = GetNode<Polygon2D>("CarriedEvolutionFruitVisual/Fruit");
		_carriedEvolutionFruitAccent = GetNode<Polygon2D>("CarriedEvolutionFruitVisual/Accent");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& !keyEvent.Echo
			&& (keyEvent.Keycode == Key.E || keyEvent.PhysicalKeycode == Key.E))
		{
			TryInteract();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Vector2.Zero;

		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			inputDirection.X -= 1.0f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			inputDirection.X += 1.0f;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			inputDirection.Y -= 1.0f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			inputDirection.Y += 1.0f;

		if (inputDirection != Vector2.Zero)
		{
			FacingDirection = inputDirection.Normalized();
			_visual.Rotation = FacingDirection.Angle() - Vector2.Down.Angle();
		}

		Velocity = inputDirection.Normalized() * MovementSpeed;
		MoveAndSlide();
	}

	private void TryInteract()
	{
		IInteractable closestInteractable = null;
		float closestDistanceSquared = float.MaxValue;

		foreach (Node2D body in _interactionArea.GetOverlappingBodies())
		{
			if (body is not IInteractable interactable)
				continue;

			float distanceSquared = GlobalPosition.DistanceSquaredTo(body.GlobalPosition);
			if (distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestInteractable = interactable;
			}
		}

		foreach (Area2D area in _interactionArea.GetOverlappingAreas())
		{
			if (area is not IInteractable interactable)
				continue;

			float distanceSquared = GlobalPosition.DistanceSquaredTo(area.GlobalPosition);
			if (distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestInteractable = interactable;
			}
		}

		closestInteractable?.TryInteract(this);
	}

	public bool TryPickupFood()
	{
		return TryPickupItem(CarriedItem.CreateFood());
	}

	public bool TryConsumeCarriedFood()
	{
		if (!IsCarryingFood)
			return false;

		TryTakeCarriedItem(out _);
		return true;
	}

	public bool TryPickupItem(CarriedItem item)
	{
		if (item == null || CarriedItem != null)
			return false;

		CarriedItem = item;
		UpdateCarriedItemVisual();
		return true;
	}

	public bool TryTakeCarriedItem(out CarriedItem item)
	{
		item = CarriedItem;
		if (item == null)
			return false;

		CarriedItem = null;
		UpdateCarriedItemVisual();
		return true;
	}

	public void SetCarriedItem(CarriedItem item)
	{
		CarriedItem = item;
		UpdateCarriedItemVisual();
	}

	public void SetCarryingFood(bool isCarryingFood)
	{
		SetCarriedItem(isCarryingFood ? CarriedItem.CreateFood() : null);
	}

	private void UpdateCarriedItemVisual()
	{
		_carriedFoodVisual.Visible = CarriedItem?.Kind == CarriedItemKind.Food;
		_carriedCrystalVisual.Visible = CarriedItem?.Kind == CarriedItemKind.Crystal;
		_carriedEvolutionFruitVisual.Visible = CarriedItem?.Kind == CarriedItemKind.EvolutionFruit;

		if (CarriedItem?.Kind == CarriedItemKind.EvolutionFruit && CarriedItem.DevelopmentType.HasValue)
		{
			(_carriedEvolutionFruitShape.Color, _carriedEvolutionFruitAccent.Color) =
				CarriedItem.DevelopmentType.Value switch
				{
					CreatureDevelopmentType.Star => (new Color(0.98f, 0.75f, 0.2f), new Color(1.0f, 0.96f, 0.58f)),
					CreatureDevelopmentType.Natural => (new Color(0.3f, 0.75f, 0.3f), new Color(0.72f, 0.95f, 0.42f)),
					CreatureDevelopmentType.Void => (new Color(0.38f, 0.2f, 0.62f), new Color(0.82f, 0.45f, 0.96f)),
					_ => (Colors.White, Colors.White)
				};
		}

		if (CarriedItem?.Kind != CarriedItemKind.Crystal)
			return;

		bool isRuby = CarriedItem.StatType == CreatureStatType.Power;
		_carriedCrystalShape.Color = isRuby
			? new Color(0.88f, 0.16f, 0.22f)
			: new Color(0.16f, 0.78f, 0.38f);
		_carriedCrystalHighlight.Color = isRuby
			? new Color(1.0f, 0.62f, 0.62f, 0.9f)
			: new Color(0.68f, 1.0f, 0.74f, 0.9f);
	}
}
