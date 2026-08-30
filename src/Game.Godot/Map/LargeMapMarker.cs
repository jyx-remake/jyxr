using Game.Core.Definitions;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapMarker : Control
{
	private static readonly Vector2 DefaultVisualPosition = new(-40f, -80f);
	private static readonly Vector2 DefaultVisualSize = new(80f, 80f);
	private static readonly Vector2 CompactTownVisualSize = new(56f, 56f);
	private static readonly Vector2 DefaultNamePosition = new(-35f, 70f);
	private static readonly Vector2 DefaultNameSize = new(150f, 36f);
	private static readonly Vector2 DefaultNoticePosition = new(59f, -11f);

	[Export]
	public Texture2D? DefaultTexture { get; set; }

	private TextureRect _avatar = null!;
	private LargeMapOutline _outline = null!;
	private Control _visual = null!;
	private OverflowTextureRect _overflowAvatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;

	public (string MapId, MapLocationDefinition Location, MapEventDefinition? Event)? Location { get; private set; }

	public Vector2 LogicalPosition { get; private set; }

	public override void _Ready()
	{
		_visual = GetNode<Control>("%Visual");
		_outline = GetNode<LargeMapOutline>("%Outline");
		_avatar = GetNode<TextureRect>("%Avatar");
		_overflowAvatar = GetNode<OverflowTextureRect>("%OverflowAvatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		Refresh();
	}

	public void Setup(
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location,
		Vector2 logicalPosition)
	{
		Location = location;
		LogicalPosition = logicalPosition;
		Refresh();
	}

	public Rect2 GetScreenBounds() => new(
		Position + _visual.Position * Scale,
		_visual.Size * Scale);

	private void Refresh()
	{
		if (Location is not { } location || !IsInsideTree())
		{
			return;
		}

		_nameLabel.Text = MapEntityPresentation.ResolveLocationName(location.Location);
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		var avatar = MapEntityPresentation.ResolveAvatar(
			DefaultTexture,
			location.Location,
			location.Event);
		ApplyAvatarLayout(avatar);
		_avatar.Texture = avatar.UseOverflow ? null : avatar.Texture;
		_avatar.Visible = !avatar.UseOverflow;
		_overflowAvatar.Texture = avatar.UseOverflow ? avatar.Texture : null;
		_overflowAvatar.Visible = avatar.UseOverflow;
	}

	private void ApplyAvatarLayout(MapEntityAvatarPresentation avatar)
	{
		if (!avatar.UseNativeSize || avatar.Texture is null)
		{
			var visualSize = avatar.UseCompactTownSize ? CompactTownVisualSize : DefaultVisualSize;
			_visual.Position = avatar.UseCompactTownSize
				? new Vector2(-visualSize.X * 0.5f, -visualSize.Y)
				: DefaultVisualPosition;
			_visual.Size = visualSize;
			_nameLabel.Position = avatar.UseCompactTownSize
				? new Vector2((visualSize.X - DefaultNameSize.X) * 0.5f, visualSize.Y + 8f)
				: DefaultNamePosition;
			_nameLabel.Size = DefaultNameSize;
			_notice.Position = avatar.UseCompactTownSize
				? new Vector2(visualSize.X - 21f, -11f)
				: DefaultNoticePosition;
			ApplyOutlineLayout(avatar.Texture, avatar.UseOverflow);
			return;
		}

		var textureSize = avatar.Texture.GetSize();
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			ApplyOutlineLayout(null, false);
			return;
		}

		// town.native.* and town.city.* are consumed by the legacy launcher at
		// their texture dimensions. Keep the marker's bottom-center anchor while
		// allowing those resources to extend beyond the regular 80x80 slot.
		_visual.Size = textureSize;
		_visual.Position = new Vector2(-textureSize.X * 0.5f, -textureSize.Y);
		// NameLabel is a child of Visual. Its local X must be calculated from
		// the native texture width; using the regular -75 offset would apply the
		// visual's -width/2 anchor a second time and shift the caption left.
		_nameLabel.Position = new Vector2(
			(textureSize.X - DefaultNameSize.X) * 0.5f,
			textureSize.Y + 8f);
		_nameLabel.Size = DefaultNameSize;
		_notice.Position = new Vector2(textureSize.X - 21f, -11f);
		ApplyOutlineLayout(avatar.Texture, avatar.UseOverflow);
	}

	private void ApplyOutlineLayout(Texture2D? texture, bool useOverflow)
	{
		_outline.Texture = texture;
		_outline.UseAspectCover = useOverflow;
		_outline.Visible = texture is not null;
	}
}
