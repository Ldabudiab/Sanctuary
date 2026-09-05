using Godot;

public enum CreatureEvolutionType
{
	Base,
	Star,
	Natural,
	Void
}

public partial class CreatureEvolution : Node
{
	public const int FirstEvolutionAge = 100;

	public CreatureEvolutionType CurrentForm { get; private set; } = CreatureEvolutionType.Base;

	public bool TrySelectFirstEvolution(int age, CreatureDevelopment development)
	{
		if (CurrentForm != CreatureEvolutionType.Base || age < FirstEvolutionAge)
			return false;

		CurrentForm = SelectFirstEvolution(development.Star, development.Natural, development.Void);
		return true;
	}

	public void RestoreSavedForm(CreatureEvolutionType form)
	{
		CurrentForm = System.Enum.IsDefined(form) ? form : CreatureEvolutionType.Base;
	}

	public static CreatureEvolutionType SelectFirstEvolution(float star, float natural, float voidDevelopment)
	{
		if (star > natural && star > voidDevelopment)
			return CreatureEvolutionType.Star;

		if (voidDevelopment > star && voidDevelopment > natural)
			return CreatureEvolutionType.Void;

		// Natural wins when strictly highest and for every two-way or three-way tie.
		return CreatureEvolutionType.Natural;
	}
}
