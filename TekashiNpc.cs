using Godot;

public partial class TekashiNpc : Area2D, IInteractable
{
	[Export]
	public NodePath DialoguePath { get; set; } = null!;

	private Label _prompt = null!;
	private TekashiDialogue _dialogue = null!;
	private Player _nearbyPlayer = null!;

	public override void _Ready()
	{
		_prompt = GetNode<Label>("Prompt");
		_dialogue = GetNode<TekashiDialogue>(DialoguePath);
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		_prompt.Visible = IsInstanceValid(_nearbyPlayer) && _nearbyPlayer.IsGameplayInputEnabled;
	}

	public bool TryInteract(Node interactor)
	{
		if (interactor is not Player player || player != _nearbyPlayer || !player.IsGameplayInputEnabled)
			return false;

		_prompt.Visible = false;
		_dialogue.Open(player);
		return true;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

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
