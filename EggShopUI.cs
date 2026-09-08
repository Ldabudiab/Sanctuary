using Godot;

public partial class EggShopUI : Control
{
	[Export]
	public string ShopTitle { get; set; } = "Tekashi's Eggs";

	[Export]
	public string ListingName { get; set; } = "Puca Egg";

	[Export]
	public string ListingPrice { get; set; } = "100 G";

	private Player _player = null!;
	private Label _title = null!;
	private Button _listing = null!;
	private Label _feedback = null!;
	private Button _buyButton = null!;
	private Button _exitButton = null!;

	public override void _Ready()
	{
		_title = GetNode<Label>("Window/Margin/Layout/Title");
		_listing = GetNode<Button>("Window/Margin/Layout/Listing");
		_feedback = GetNode<Label>("Window/Margin/Layout/Feedback");
		_buyButton = GetNode<Button>("Window/Margin/Layout/Actions/Buy");
		_exitButton = GetNode<Button>("Window/Margin/Layout/Actions/Exit");

		_title.Text = ShopTitle;
		_listing.Text = $"{ListingName}\n{ListingPrice}";
		_listing.Toggled += OnListingToggled;
		_buyButton.Pressed += OnBuyPressed;
		_exitButton.Pressed += Close;
		UpdateSelection();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible
			|| @event is not InputEventKey keyEvent
			|| !keyEvent.Pressed
			|| keyEvent.Echo
			|| (keyEvent.Keycode != Key.Escape && keyEvent.PhysicalKeycode != Key.Escape))
		{
			return;
		}

		Close();
		GetViewport().SetInputAsHandled();
	}

	public void Open(Player player)
	{
		_player = player;
		_player.SetGameplayInputEnabled(false);
		_feedback.Text = "Select an egg.";
		_listing.ButtonPressed = false;
		Visible = true;
		UpdateSelection();
	}

	public void Close()
	{
		Visible = false;
		if (IsInstanceValid(_player))
			_player.SetGameplayInputEnabled(true);
		_player = null!;
	}

	private void OnListingToggled(bool selected)
	{
		UpdateSelection();
	}

	private void OnBuyPressed()
	{
		if (!_listing.ButtonPressed)
		{
			_feedback.Text = "Select the Puca Egg first.";
			return;
		}

		_feedback.Text = _player.Inventory.Add(ItemCatalog.PucaEgg)
			? "Puca Egg purchased."
			: "Inventory full.";
	}

	private void UpdateSelection()
	{
		_listing.Modulate = _listing.ButtonPressed
			? new Color(1.0f, 0.88f, 0.48f)
			: Colors.White;
		_buyButton.Disabled = false;
	}
}
