using Godot;

public partial class PlayerVisualController : Node2D
{
	private const int DirectionCount = 8;
	private const int IdleRowCount = 3;
	private const int WalkFrameCount = 3;
	private const int RightDirectionIndex = 2;
	private const int LeftDirectionIndex = 6;
	private const float WalkPosesPerSecond = 8.0f;
	private static readonly Vector2 BaseSpritePosition = new(0.0f, -21.0f);
	private static readonly int[] WalkFrameSequence = { 0, 1, 2, 1 };

	[Export]
	public Texture2D IdleTexture { get; set; } = null!;

	[Export]
	public Texture2D WalkTexture { get; set; } = null!;

	private Player _player = null!;
	private Sprite2D _sprite = null!;
	private float _walkTime;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_sprite = GetNode<Sprite2D>("CharacterSprite");
		ApplyIdleFrame(0);
	}

	public override void _Process(double delta)
	{
		bool isWalking = _player.Velocity.LengthSquared() > 1.0f;
		int direction = GetDirectionIndex(_player.FacingDirection);

		if (!isWalking)
		{
			_walkTime = 0.0f;
			_sprite.Position = BaseSpritePosition;
			_sprite.Rotation = 0.0f;
			ApplyIdleFrame(direction);
			return;
		}

		_walkTime += (float)delta;
		int walkPose = Mathf.FloorToInt(_walkTime * WalkPosesPerSecond) % WalkFrameSequence.Length;
		_sprite.Position = BaseSpritePosition;
		_sprite.Rotation = 0.0f;
		ApplyWalkFrame(direction, WalkFrameSequence[walkPose]);
	}

	private void ApplyIdleFrame(int direction)
	{
		ApplyFrame(IdleTexture, IdleRowCount, direction, 0);
	}

	private void ApplyWalkFrame(int direction, int walkFrame)
	{
		ApplyFrame(WalkTexture, WalkFrameCount, direction, walkFrame);
	}

	private void ApplyFrame(Texture2D texture, int rowCount, int direction, int animationRow)
	{
		bool mirrorLeftProfileForRight = direction == RightDirectionIndex;
		int atlasDirection = mirrorLeftProfileForRight ? LeftDirectionIndex : direction;
		_sprite.FlipH = mirrorLeftProfileForRight;
		_sprite.Texture = texture;

		Vector2 textureSize = texture.GetSize();
		Vector2 frameSize = new(textureSize.X / DirectionCount, textureSize.Y / rowCount);
		_sprite.RegionRect = new Rect2(
			new Vector2(atlasDirection * frameSize.X, animationRow * frameSize.Y),
			frameSize);
	}

	private static int GetDirectionIndex(Vector2 facingDirection)
	{
		if (facingDirection == Vector2.Zero)
			return 0;

		float eighthTurn = Mathf.Pi / 4.0f;
		int direction = Mathf.RoundToInt((Mathf.Pi / 2.0f - facingDirection.Angle()) / eighthTurn);
		return Mathf.PosMod(direction, DirectionCount);
	}
}
