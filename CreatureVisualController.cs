using Godot;

public partial class CreatureVisualController : Node2D
{
	private const int FrameWidth = 160;
	private const int FrameHeight = 160;

	[Export]
	public Texture2D BaseAnimationSheet { get; set; } = null!;

	[Export]
	public Texture2D StarAnimationSheet { get; set; } = null!;

	[Export]
	public Texture2D VoidAnimationSheet { get; set; } = null!;

	private Creature _creature = null!;
	private AnimatedSprite2D _sprite = null!;
	private float _actionOverrideTime;
	private CreatureEvolutionType _currentForm = CreatureEvolutionType.Base;

	public override void _Ready()
	{
		_creature = GetParent<Creature>();
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = BuildSpriteFrames(BaseAnimationSheet);
		PlayIfChanged("idle");
	}

	public override void _Process(double delta)
	{
		if (Mathf.Abs(_creature.Velocity.X) > 1.0f)
			FaceHorizontal(_creature.Velocity.X);

		if (_actionOverrideTime > 0.0f)
		{
			_actionOverrideTime = Mathf.Max(0.0f, _actionOverrideTime - (float)delta);
			if (_actionOverrideTime > 0.0f)
				return;
		}

		PlayIfChanged(SelectAnimation());
	}

	public void FaceHorizontal(float horizontalDirection)
	{
		if (Mathf.Abs(horizontalDirection) > 0.01f)
			_sprite.FlipH = horizontalDirection < 0.0f;
	}

	public void PlayAttack()
	{
		PlayActionOverride("attack", 0.55f);
	}

	public void PlayHurt()
	{
		PlayActionOverride("hurt", 0.45f);
	}

	public void SetEvolutionForm(CreatureEvolutionType form)
	{
		Texture2D selectedSheet = form switch
		{
			CreatureEvolutionType.Star => StarAnimationSheet,
			CreatureEvolutionType.Void => VoidAnimationSheet,
			_ => BaseAnimationSheet
		};

		if (_currentForm == form && _sprite.SpriteFrames != null)
			return;

		_currentForm = form;
		_sprite.SpriteFrames = BuildSpriteFrames(selectedSheet);
		PlayIfChanged(SelectAnimation());
	}

	private string SelectAnimation()
	{
		string state = _creature.CurrentAiState;

		if (state.Contains("Sleep") || state.Contains("Nap"))
			return "sleep";
		if (state.Contains("Petting") || state.Contains("StatBoost"))
			return "happy";
		if (state.Contains("Eating"))
			return "eat";

		if (_creature.Velocity.LengthSquared() > 1.0f)
		{
			if (state == "Racing" || state.Contains("Running"))
				return "run";
			return "walk";
		}

		return "idle";
	}

	private void PlayActionOverride(string animation, float duration)
	{
		_actionOverrideTime = duration;
		_sprite.Play(animation);
	}

	private void PlayIfChanged(string animation)
	{
		if (_sprite.Animation == animation && _sprite.IsPlaying())
			return;

		_sprite.Play(animation);
	}

	private SpriteFrames BuildSpriteFrames(Texture2D animationSheet)
	{
		SpriteFrames frames = new();
		frames.RemoveAnimation("default");
		AddAnimation(frames, animationSheet, "idle", 0, 4, 3.5f, true);
		AddAnimation(frames, animationSheet, "walk", 1, 6, 8.0f, true);
		AddAnimation(frames, animationSheet, "run", 2, 6, 11.0f, true);
		AddAnimation(frames, animationSheet, "sleep", 3, 6, 4.0f, true);
		AddAnimation(frames, animationSheet, "happy", 4, 4, 6.0f, true);
		AddAnimation(frames, animationSheet, "eat", 5, 6, 7.0f, true);
		AddAnimation(frames, animationSheet, "attack", 6, 6, 11.0f, false);
		AddAnimation(frames, animationSheet, "hurt", 7, 4, 9.0f, false);
		return frames;
	}

	private void AddAnimation(
		SpriteFrames frames,
		Texture2D animationSheet,
		StringName animationName,
		int row,
		int frameCount,
		float framesPerSecond,
		bool loop)
	{
		frames.AddAnimation(animationName);
		frames.SetAnimationSpeed(animationName, framesPerSecond);
		frames.SetAnimationLoopMode(
			animationName,
			loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);

		for (int column = 0; column < frameCount; column++)
		{
			AtlasTexture frame = new()
			{
				Atlas = animationSheet,
				Region = new Rect2(column * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight),
				FilterClip = true
			};
			frames.AddFrame(animationName, frame);
		}
	}
}
