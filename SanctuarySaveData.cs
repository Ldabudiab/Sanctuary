using System.Collections.Generic;

public sealed class SanctuarySaveData
{
	public int Version { get; set; } = 1;
	public Dictionary<string, CreatureSaveData> Creatures { get; set; } = new();
}

public sealed class CreatureSaveData
{
	public CreatureStatsSaveData Stats { get; set; } = new();
	public CreaturePersonalitySaveData Personality { get; set; } = new();
	public CreatureNeedsSaveData Needs { get; set; } = new();
}

public sealed class CreatureStatsSaveData
{
	public float Speed { get; set; }
	public float Power { get; set; }
	public float Endurance { get; set; }
	public float Swimming { get; set; }
	public float Intelligence { get; set; }
}

public sealed class CreaturePersonalitySaveData
{
	public float Activity { get; set; }
	public float Attachment { get; set; }
	public float Curiosity { get; set; }
	public float Social { get; set; }
	public float Temperament { get; set; }
}

public sealed class CreatureNeedsSaveData
{
	public float Hunger { get; set; }
	public float Happiness { get; set; }
	public float Energy { get; set; }
}
