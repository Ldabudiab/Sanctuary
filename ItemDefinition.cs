public sealed class ItemDefinition
{
	public string Id { get; }
	public string DisplayName { get; }

	public ItemDefinition(string id, string displayName)
	{
		Id = id;
		DisplayName = displayName;
	}
}

public static class ItemCatalog
{
	public static readonly ItemDefinition PucaEgg = new("puca_egg", "Puca Egg");

	public static ItemDefinition Get(string itemId)
	{
		return itemId switch
		{
			"puca_egg" => PucaEgg,
			_ => new ItemDefinition(itemId, itemId)
		};
	}
}
