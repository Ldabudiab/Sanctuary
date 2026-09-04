public enum CarriedItemKind
{
	Food,
	Crystal
}

public sealed class CarriedItem
{
	public CarriedItemKind Kind { get; }
	public CreatureStatType? StatType { get; }
	public float StatIncrease { get; }

	private CarriedItem(CarriedItemKind kind, CreatureStatType? statType = null, float statIncrease = 0.0f)
	{
		Kind = kind;
		StatType = statType;
		StatIncrease = statIncrease;
	}

	public static CarriedItem CreateFood()
	{
		return new CarriedItem(CarriedItemKind.Food);
	}

	public static CarriedItem CreateCrystal(CreatureStatType statType, float statIncrease)
	{
		return new CarriedItem(CarriedItemKind.Crystal, statType, statIncrease);
	}
}
