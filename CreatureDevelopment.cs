using Godot;

public enum CreatureDevelopmentType
{
	Star,
	Natural,
	Void
}

public partial class CreatureDevelopment : Node
{
	public const float MaximumDevelopment = 100.0f;

	public float Star { get; private set; }
	public float Natural { get; private set; }
	public float Void { get; private set; }

	public bool ApplyIncrease(CreatureDevelopmentType type, float amount)
	{
		if (amount <= 0.0f)
			return false;

		float previousValue = GetValue(type);
		float newValue = Mathf.Clamp(previousValue + amount, 0.0f, MaximumDevelopment);
		SetValue(type, newValue);
		return previousValue < MaximumDevelopment && newValue >= MaximumDevelopment;
	}

	public float GetValue(CreatureDevelopmentType type)
	{
		return type switch
		{
			CreatureDevelopmentType.Star => Star,
			CreatureDevelopmentType.Natural => Natural,
			CreatureDevelopmentType.Void => Void,
			_ => 0.0f
		};
	}

	public void ApplySavedValues(float star, float natural, float voidDevelopment)
	{
		Star = Mathf.Clamp(star, 0.0f, MaximumDevelopment);
		Natural = Mathf.Clamp(natural, 0.0f, MaximumDevelopment);
		Void = Mathf.Clamp(voidDevelopment, 0.0f, MaximumDevelopment);
	}

	private void SetValue(CreatureDevelopmentType type, float value)
	{
		switch (type)
		{
			case CreatureDevelopmentType.Star:
				Star = value;
				break;
			case CreatureDevelopmentType.Natural:
				Natural = value;
				break;
			case CreatureDevelopmentType.Void:
				Void = value;
				break;
		}
	}
}
