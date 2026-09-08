using Godot;

public partial class EggNest : Area2D, IInteractable
{
	[Export]
	public string PersistentId { get; set; } = string.Empty;

	public bool HasEgg { get; private set; }
	public double IncubationStartedAt { get; private set; }

	private Player _nearbyPlayer = null!;
	private Label _prompt = null!;
	private Label _message = null!;
	private CanvasItem _eggVisual = null!;
	private float _messageTimeRemaining;

	public override void _Ready()
	{
		_prompt = GetNode<Label>("Prompt");
		_message = GetNode<Label>("Message");
		_eggVisual = GetNode<CanvasItem>("Egg");
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		RefreshVisuals();
	}

	public override void _Process(double delta)
	{
		_prompt.Visible = !HasEgg
			&& IsInstanceValid(_nearbyPlayer)
			&& _nearbyPlayer.IsGameplayInputEnabled;

		if (_messageTimeRemaining <= 0.0f)
			return;

		_messageTimeRemaining -= (float)delta;
		if (_messageTimeRemaining <= 0.0f)
			_message.Visible = false;
	}

	public bool TryInteract(Node interactor)
	{
		if (HasEgg || interactor is not Player player || player != _nearbyPlayer)
			return false;

		EggAcquisitionManager manager = GetTree().GetFirstNodeInGroup("egg_acquisition_manager") as EggAcquisitionManager;
		return manager != null && manager.TryPlaceEgg(this, player);
	}

	public void BeginIncubation(double totalWorldTime)
	{
		HasEgg = true;
		IncubationStartedAt = totalWorldTime;
		RefreshVisuals();
	}

	public void ClearEgg()
	{
		HasEgg = false;
		IncubationStartedAt = 0.0;
		RefreshVisuals();
	}

	public EggNestSaveData CreateSaveData()
	{
		return new EggNestSaveData
		{
			HasEgg = HasEgg,
			IncubationStartedAt = IncubationStartedAt
		};
	}

	public void Restore(EggNestSaveData data)
	{
		HasEgg = data?.HasEgg == true;
		IncubationStartedAt = HasEgg ? data.IncubationStartedAt : 0.0;
		RefreshVisuals();
	}

	public void ShowMessage(string text)
	{
		_message.Text = text;
		_message.Visible = true;
		_messageTimeRemaining = 2.0f;
	}

	private void RefreshVisuals()
	{
		if (IsInstanceValid(_eggVisual))
			_eggVisual.Visible = HasEgg;
		if (IsInstanceValid(_prompt) && HasEgg)
			_prompt.Visible = false;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
			_nearbyPlayer = player;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body != _nearbyPlayer)
			return;
		_nearbyPlayer = null!;
		_prompt.Visible = false;
	}
}
