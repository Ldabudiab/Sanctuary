using Godot;

public enum CreatureStatType
{
	Speed,
	Power,
	Endurance,
	Swimming,
	Intelligence
}

public partial class CreatureStats : Node
{
	public const float MaximumStatValue = 100.0f;

	public float Speed { get; private set; }
	public float Power { get; private set; }
	public float Endurance { get; private set; }
	public float Swimming { get; private set; }
	public float Intelligence { get; private set; }

	public void ApplyIncrease(CreatureStatType stat, float amount)
	{
		if (amount <= 0.0f)
			return;

		switch (stat)
		{
			case CreatureStatType.Speed:
				Speed = ClampStat(Speed + amount);
				break;
			case CreatureStatType.Power:
				Power = ClampStat(Power + amount);
				break;
			case CreatureStatType.Endurance:
				Endurance = ClampStat(Endurance + amount);
				break;
			case CreatureStatType.Swimming:
				Swimming = ClampStat(Swimming + amount);
				break;
			case CreatureStatType.Intelligence:
				Intelligence = ClampStat(Intelligence + amount);
				break;
		}
	}

	private static float ClampStat(float value)
	{
		return Mathf.Clamp(value, 0.0f, MaximumStatValue);
	}
}
