using Godot;

public partial class CreatureNeeds : Node
{
	[Export(PropertyHint.Range, "0,100,0.1")]
	public float StartingHunger { get; set; } = 100.0f;

	[Export(PropertyHint.Range, "0,100,0.1")]
	public float StartingHappiness { get; set; } = 50.0f;

	[Export]
	public float HungerDecayPerSecond { get; set; } = 0.5f;

	[Export]
	public float PetHappinessGain { get; set; } = 10.0f;

	[Export]
	public float FoodHungerRestore { get; set; } = 30.0f;

	public float Hunger { get; private set; }
	public float Happiness { get; private set; }

	public override void _Ready()
	{
		Hunger = Mathf.Clamp(StartingHunger, 0.0f, 100.0f);
		Happiness = Mathf.Clamp(StartingHappiness, 0.0f, 100.0f);
	}

	public override void _Process(double delta)
	{
		Hunger = Mathf.Clamp(Hunger - HungerDecayPerSecond * (float)delta, 0.0f, 100.0f);
	}

	public void ApplyPetting()
	{
		Happiness = Mathf.Clamp(Happiness + PetHappinessGain, 0.0f, 100.0f);
	}

	public void ApplyFeeding()
	{
		Hunger = Mathf.Clamp(Hunger + FoodHungerRestore, 0.0f, 100.0f);
	}
}
