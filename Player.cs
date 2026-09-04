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
	private CanvasItem _carriedEmeraldVisual = null!;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_carriedFoodVisual = GetNode<CanvasItem>("CarriedFoodVisual");
		_carriedEmeraldVisual = GetNode<CanvasItem>("CarriedEmeraldVisual");
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
		_carriedEmeraldVisual.Visible = CarriedItem?.Kind == CarriedItemKind.Crystal;
	}
}
