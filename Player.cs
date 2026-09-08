using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MovementSpeed { get; set; } = 200.0f;

	public Vector2 FacingDirection { get; private set; } = Vector2.Down;
	public bool IsGameplayInputEnabled { get; private set; } = true;
	public Inventory Inventory => PlayerInventory.Current;

	private Node2D _visual = null!;
	private Area2D _interactionArea = null!;
	private InventoryUI _inventoryUi = null!;

	public override void _Ready()
	{
		_visual = GetNode<Node2D>("Visual");
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_inventoryUi = GetNode<InventoryUI>("InventoryUI");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsGameplayInputEnabled
			&& @event is InputEventKey keyEvent
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
		if (!IsGameplayInputEnabled)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

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

	public bool TryCollectItem(ItemDefinition item)
	{
		if (Inventory.Add(item))
			return true;
		_inventoryUi.ShowFeedback("Inventory full.");
		return false;
	}

	public void SetGameplayInputEnabled(bool enabled)
	{
		IsGameplayInputEnabled = enabled;
		if (!enabled)
			Velocity = Vector2.Zero;
	}

}
