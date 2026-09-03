using Godot;

public partial class CreatureVisualController : Node2D
{
	private static readonly Vector2 BaseSpritePosition = new(0.0f, -2.25f);
	private static readonly Vector2 BaseSpriteScale = new(0.03375f, 0.03375f);
	private static readonly Vector2 BaseIndicatorPosition = new(0.0f, -29.0f);
	private static readonly Vector2 RenderedTextureSize = new(49.6125f, 36.1125f);

	private CharacterBody2D _creature = null!;
	private Sprite2D _sprite = null!;
	private Node2D _indicator = null!;
	private ShaderMaterial _material = null!;
	private Node2D _torsoPart = null!;
	private Node2D _headPart = null!;
	private Node2D _leftAppendagePart = null!;
	private Node2D _rightAppendagePart = null!;
	private Node2D _leftFootPart = null!;
	private Node2D _rightFootPart = null!;
	private Vector2 _indicatorOffset;
	private Vector2 _indicatorVelocity;
	private float _animationTime;

	public override void _Ready()
	{
		_creature = GetParent<CharacterBody2D>();
		_sprite = GetNode<Sprite2D>("BaseSprite");
		_indicator = GetNode<Node2D>("../NeutralIndicator");
		_material = (ShaderMaterial)_sprite.Material;
		_torsoPart = GetNode<Node2D>("Rig/TorsoPart");
		_headPart = GetNode<Node2D>("Rig/HeadPart");
		_leftAppendagePart = GetNode<Node2D>("Rig/LeftAppendagePart");
		_rightAppendagePart = GetNode<Node2D>("Rig/RightAppendagePart");
		_leftFootPart = GetNode<Node2D>("Rig/LeftFootPart");
		_rightFootPart = GetNode<Node2D>("Rig/RightFootPart");
	}

	public override void _Process(double delta)
	{
		float frameDelta = (float)delta;
		_animationTime += frameDelta;
		bool isWalking = _creature.Velocity.LengthSquared() > 1.0f;

		if (Mathf.Abs(_creature.Velocity.X) > 1.0f)
			_sprite.FlipH = _creature.Velocity.X < 0.0f;

		if (isWalking)
			AnimateWalk(frameDelta);
		else
			AnimateIdle(frameDelta);

		ApplyRigToMaterial();
	}

	private void AnimateIdle(float delta)
	{
		float phase = _animationTime * 1.8f;
		float breath = Mathf.Sin(phase);

		_sprite.Position = BaseSpritePosition;
		_sprite.Scale = BaseSpriteScale;
		_torsoPart.Position = new Vector2(Mathf.Sin(phase * 0.45f) * 0.08f, -breath * 0.10f);
		_torsoPart.Scale = new Vector2(1.0f - breath * 0.002f, 1.0f + breath * 0.006f);
		_headPart.Position = new Vector2(Mathf.Sin(phase * 0.45f - 0.28f) * 0.12f, -Mathf.Sin(phase - 0.22f) * 0.22f);
		_leftAppendagePart.Position = new Vector2(Mathf.Sin(phase * 0.72f + 0.4f) * 0.28f, Mathf.Sin(phase * 0.58f + 1.1f) * 0.18f);
		_rightAppendagePart.Position = new Vector2(Mathf.Sin(phase * 0.67f + 2.5f) * 0.24f, Mathf.Sin(phase * 0.63f + 0.2f) * 0.21f);
		_leftFootPart.Position = Vector2.Zero;
		_rightFootPart.Position = Vector2.Zero;
		UpdateIndicatorFollow(_headPart.Position * 0.65f + Vector2.Up * Mathf.Sin(phase * 0.7f) * 0.45f, delta);
	}

	private void AnimateWalk(float delta)
	{
		float phase = _animationTime * 8.0f;
		float weightShift = Mathf.Sin(phase);
		float contact = Mathf.Abs(weightShift);
		float leftLift = Mathf.Max(0.0f, weightShift);
		float rightLift = Mathf.Max(0.0f, -weightShift);

		_sprite.Position = BaseSpritePosition;
		_sprite.Scale = BaseSpriteScale;
		_torsoPart.Position = new Vector2(weightShift * 0.38f, -contact * 0.28f);
		_torsoPart.Scale = new Vector2(1.0f + contact * 0.006f, 1.0f - contact * 0.012f);
		_headPart.Position = new Vector2(Mathf.Sin(phase - 0.42f) * 0.24f, -Mathf.Abs(Mathf.Sin(phase - 0.30f)) * 0.42f);
		_leftFootPart.Position = new Vector2(-weightShift * 0.12f, -leftLift * 1.15f);
		_rightFootPart.Position = new Vector2(-weightShift * 0.12f, -rightLift * 1.15f);
		_leftAppendagePart.Position = new Vector2(-weightShift * 0.48f + Mathf.Sin(phase - 0.75f) * 0.18f, Mathf.Sin(phase - 0.55f) * 0.28f);
		_rightAppendagePart.Position = new Vector2(-weightShift * 0.43f + Mathf.Sin(phase - 0.98f) * 0.16f, Mathf.Sin(phase - 0.82f) * 0.25f);
		UpdateIndicatorFollow(_headPart.Position * 0.75f + new Vector2(-weightShift * 0.12f, -contact * 0.75f), delta);
	}

	private void UpdateIndicatorFollow(Vector2 targetOffset, float delta)
	{
		_indicatorVelocity += (targetOffset - _indicatorOffset) * 28.0f * delta;
		_indicatorVelocity *= Mathf.Exp(-7.0f * delta);
		_indicatorOffset += _indicatorVelocity * delta;
		_indicator.Position = BaseIndicatorPosition + _indicatorOffset;
	}

	private void ApplyRigToMaterial()
	{
		_material.SetShaderParameter("torso_offset", ToUvOffset(_torsoPart.Position));
		_material.SetShaderParameter("head_offset", ToUvOffset(_headPart.Position));
		_material.SetShaderParameter("left_appendage_offset", ToUvOffset(_leftAppendagePart.Position));
		_material.SetShaderParameter("right_appendage_offset", ToUvOffset(_rightAppendagePart.Position));
		_material.SetShaderParameter("left_foot_offset", ToUvOffset(_leftFootPart.Position));
		_material.SetShaderParameter("right_foot_offset", ToUvOffset(_rightFootPart.Position));
		_material.SetShaderParameter("torso_squash", _torsoPart.Scale.Y - 1.0f);
	}

	private static Vector2 ToUvOffset(Vector2 pixelOffset)
	{
		return new Vector2(pixelOffset.X / RenderedTextureSize.X, pixelOffset.Y / RenderedTextureSize.Y);
	}
}
