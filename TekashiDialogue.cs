using Godot;

public partial class TekashiDialogue : Control
{
	[Export]
	public NodePath ShopPath { get; set; } = null!;

	private EggShopUI _shop = null!;
	private Player _player = null!;

	public override void _Ready()
	{
		_shop = GetNode<EggShopUI>(ShopPath);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible
			|| @event is not InputEventKey keyEvent
			|| !keyEvent.Pressed
			|| keyEvent.Echo
			|| (keyEvent.Keycode != Key.E && keyEvent.PhysicalKeycode != Key.E))
		{
			return;
		}

		Visible = false;
		_shop.Open(_player);
		GetViewport().SetInputAsHandled();
	}

	public void Open(Player player)
	{
		_player = player;
		_player.SetGameplayInputEnabled(false);
		Visible = true;
	}
}
