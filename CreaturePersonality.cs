using Godot;

public partial class CreaturePersonality : Node
{
	private float _activity;
	private float _attachment;
	private float _curiosity;
	private float _social;
	private float _temperament;

	[Export(PropertyHint.Range, "-100,100,1")]
	public float Activity
	{
		get => _activity;
		set => _activity = Mathf.Clamp(value, -100.0f, 100.0f);
	}

	[Export(PropertyHint.Range, "-100,100,1")]
	public float Attachment
	{
		get => _attachment;
		set => _attachment = Mathf.Clamp(value, -100.0f, 100.0f);
	}

	[Export(PropertyHint.Range, "-100,100,1")]
	public float Curiosity
	{
		get => _curiosity;
		set => _curiosity = Mathf.Clamp(value, -100.0f, 100.0f);
	}

	[Export(PropertyHint.Range, "-100,100,1")]
	public float Social
	{
		get => _social;
		set => _social = Mathf.Clamp(value, -100.0f, 100.0f);
	}

	[Export(PropertyHint.Range, "-100,100,1")]
	public float Temperament
	{
		get => _temperament;
		set => _temperament = Mathf.Clamp(value, -100.0f, 100.0f);
	}

	public void ApplyPetting()
	{
		Attachment += 2.0f;
	}

	public void ApplyFeeding()
	{
		Attachment += 1.0f;
	}
}
