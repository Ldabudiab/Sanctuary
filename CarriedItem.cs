public enum CarriedItemKind
{
	Food,
	Crystal,
	EvolutionFruit
}

public sealed class CarriedItem
{
	public CarriedItemKind Kind { get; }
	public CreatureStatType? StatType { get; }
	public float StatIncrease { get; }
	public CreatureDevelopmentType? DevelopmentType { get; }
	public float DevelopmentIncrease { get; }
	public int AgeIncrease { get; }
	public float EnduranceIncrease { get; }
	public bool ImprovesAttachment { get; }

	private CarriedItem(
		CarriedItemKind kind,
		CreatureStatType? statType = null,
		float statIncrease = 0.0f,
		CreatureDevelopmentType? developmentType = null,
		float developmentIncrease = 0.0f,
		int ageIncrease = 0,
		float enduranceIncrease = 0.0f,
		bool improvesAttachment = false)
	{
		Kind = kind;
		StatType = statType;
		StatIncrease = statIncrease;
		DevelopmentType = developmentType;
		DevelopmentIncrease = developmentIncrease;
		AgeIncrease = ageIncrease;
		EnduranceIncrease = enduranceIncrease;
		ImprovesAttachment = improvesAttachment;
	}

	public static CarriedItem CreateFood(float enduranceIncrease = 1.0f)
	{
		return new CarriedItem(
			CarriedItemKind.Food,
			enduranceIncrease: enduranceIncrease,
			improvesAttachment: true);
	}

	public static CarriedItem CreateCrystal(CreatureStatType statType, float statIncrease)
	{
		return new CarriedItem(CarriedItemKind.Crystal, statType, statIncrease);
	}

	public static CarriedItem CreateEvolutionFruit(
		CreatureDevelopmentType developmentType,
		float developmentIncrease,
		int ageIncrease)
	{
		return new CarriedItem(
			CarriedItemKind.EvolutionFruit,
			developmentType: developmentType,
			developmentIncrease: developmentIncrease,
			ageIncrease: ageIncrease);
	}
}
