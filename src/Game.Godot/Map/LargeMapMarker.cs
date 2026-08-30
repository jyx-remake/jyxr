using Game.Core.Definitions;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapMarker : Control
{
	[Export]
	public Texture2D? DefaultTexture { get; set; }

	private TextureRect _avatar = null!;
	private Control _visual = null!;
	private OverflowTextureRect _overflowAvatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;

	public (string MapId, MapLocationDefinition Location, MapEventDefinition? Event)? Location { get; private set; }

	public Vector2 LogicalPosition { get; private set; }

	public override void _Ready()
	{
		_visual = GetNode<Control>("%Visual");
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
		_avatar.Texture = avatar.UseOverflow ? null : avatar.Texture;
		_avatar.Visible = !avatar.UseOverflow;
		_overflowAvatar.Texture = avatar.UseOverflow ? avatar.Texture : null;
		_overflowAvatar.Visible = avatar.UseOverflow;
	}
}
