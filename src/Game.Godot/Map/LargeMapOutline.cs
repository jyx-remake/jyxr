using Godot;

namespace Game.Godot.Map;

/// <summary>
/// Draws the legacy-launcher style outline for large-map markers only.
/// The coloured icon is rendered by a separate Avatar node above this control.
/// </summary>
public partial class LargeMapOutline : Control
{
	private Texture2D? _texture;
	private bool _useAspectCover;
	private float _outlineWidth = 1.5f;
	// The eight outline passes overlap around corners, so the restrained warm
	// ink alpha keeps the edge visible without forming a heavy black cutout.
	private Color _outlineColor = new(0.10f, 0.09f, 0.08f, 0.46f);

	public Texture2D? Texture
	{
		get => _texture;
		set
		{
			if (_texture == value)
			{
				return;
			}

			_texture = value;
			QueueRedraw();
		}
	}

	public bool UseAspectCover
	{
		get => _useAspectCover;
		set
		{
			if (_useAspectCover == value)
			{
				return;
			}

			_useAspectCover = value;
			QueueRedraw();
		}
	}

	[Export(PropertyHint.Range, "0.5,4,0.25")]
	public float OutlineWidth
	{
		get => _outlineWidth;
		set
		{
			_outlineWidth = Mathf.Max(value, 0f);
			QueueRedraw();
		}
	}

	[Export]
	public Color OutlineColor
	{
		get => _outlineColor;
		set
		{
			_outlineColor = value;
			QueueRedraw();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_texture is null || Size.X <= 0f || Size.Y <= 0f || _outlineWidth <= 0f)
		{
			return;
		}

		var textureSize = _texture.GetSize();
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			return;
		}

		var scale = _useAspectCover
			? Mathf.Max(Size.X / textureSize.X, Size.Y / textureSize.Y)
			: Mathf.Min(Size.X / textureSize.X, Size.Y / textureSize.Y);
		var drawSize = textureSize * scale;
		var drawPosition = (Size - drawSize) * 0.5f;
		var diagonal = _outlineWidth * 0.70710678f;
		var offsets = new[]
		{
			new Vector2(-_outlineWidth, 0f),
			new Vector2(_outlineWidth, 0f),
			new Vector2(0f, -_outlineWidth),
			new Vector2(0f, _outlineWidth),
			new Vector2(-diagonal, -diagonal),
			new Vector2(diagonal, -diagonal),
			new Vector2(-diagonal, diagonal),
			new Vector2(diagonal, diagonal),
		};

		foreach (var offset in offsets)
		{
			DrawTextureRect(
				_texture,
				new Rect2(drawPosition + offset, drawSize),
				false,
				_outlineColor);
		}
	}
}
