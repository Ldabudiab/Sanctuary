using System.Collections.Generic;

public enum PucaItemEffectKind
{
	None,
	Food,
	Development,
	Stat
}

public sealed class ItemDefinition
{
	public string Id { get; }
	public string DisplayName { get; }
	public PucaItemEffectKind PucaEffect { get; }
	public CreatureStatType? StatType { get; }
	public float StatIncrease { get; }
	public CreatureDevelopmentType? DevelopmentType { get; }
	public float DevelopmentIncrease { get; }
	public int AgeIncrease { get; }
	public float EnduranceIncrease { get; }
	public bool ImprovesAttachment { get; }

	public ItemDefinition(
		string id,
		string displayName,
		PucaItemEffectKind pucaEffect = PucaItemEffectKind.None,
		CreatureStatType? statType = null,
		float statIncrease = 0.0f,
		CreatureDevelopmentType? developmentType = null,
		float developmentIncrease = 0.0f,
		int ageIncrease = 0,
		float enduranceIncrease = 0.0f,
		bool improvesAttachment = false)
	{
		Id = id;
		DisplayName = displayName;
		PucaEffect = pucaEffect;
		StatType = statType;
		StatIncrease = statIncrease;
		DevelopmentType = developmentType;
		DevelopmentIncrease = developmentIncrease;
		AgeIncrease = ageIncrease;
		EnduranceIncrease = enduranceIncrease;
		ImprovesAttachment = improvesAttachment;
	}
}

public static class ItemCatalog
{
	public static readonly ItemDefinition PucaEgg = new("puca_egg", "Puca Egg");
	public static readonly ItemDefinition Food = new(
		"food", "Food", PucaItemEffectKind.Food,
		enduranceIncrease: 1.0f, improvesAttachment: true);
	public static readonly ItemDefinition StarFruit = new(
		"star_fruit", "Star Fruit", PucaItemEffectKind.Development,
		developmentType: CreatureDevelopmentType.Star, developmentIncrease: 10.0f, ageIncrease: 1);
	public static readonly ItemDefinition NaturalFruit = new(
		"natural_fruit", "Natural Fruit", PucaItemEffectKind.Development,
		developmentType: CreatureDevelopmentType.Natural, developmentIncrease: 10.0f, ageIncrease: 1);
	public static readonly ItemDefinition Voidberry = new(
		"voidberry", "Voidberry", PucaItemEffectKind.Development,
		developmentType: CreatureDevelopmentType.Void, developmentIncrease: 10.0f, ageIncrease: 1);
	public static readonly ItemDefinition Emerald = new(
		"emerald", "Emerald", PucaItemEffectKind.Stat,
		statType: CreatureStatType.Speed, statIncrease: 5.0f);
	public static readonly ItemDefinition Ruby = new(
		"ruby", "Ruby", PucaItemEffectKind.Stat,
		statType: CreatureStatType.Power, statIncrease: 5.0f);

	private static readonly Dictionary<string, ItemDefinition> Items = new()
	{
		[PucaEgg.Id] = PucaEgg,
		[Food.Id] = Food,
		[StarFruit.Id] = StarFruit,
		[NaturalFruit.Id] = NaturalFruit,
		[Voidberry.Id] = Voidberry,
		[Emerald.Id] = Emerald,
		[Ruby.Id] = Ruby
	};

	public static ItemDefinition Get(string itemId)
	{
		return Items.TryGetValue(itemId, out ItemDefinition item)
			? item
			: new ItemDefinition(itemId, itemId);
	}

	public static ItemDefinition GetFruit(CreatureDevelopmentType type) => type switch
	{
		CreatureDevelopmentType.Star => StarFruit,
		CreatureDevelopmentType.Natural => NaturalFruit,
		CreatureDevelopmentType.Void => Voidberry,
		_ => null
	};

	public static ItemDefinition GetCrystal(CreatureStatType type) => type switch
	{
		CreatureStatType.Speed => Emerald,
		CreatureStatType.Power => Ruby,
		_ => null
	};
}
