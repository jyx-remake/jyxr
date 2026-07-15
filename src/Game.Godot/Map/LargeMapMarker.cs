using Game.Core.Definitions;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapMarker : Control
{
	[Export]
	public Texture2D? DefaultTexture { get; set; }

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;

	public (string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)? Location { get; private set; }

	public Vector2 DesignPosition { get; private set; }

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		Refresh();
	}

	public void Setup(
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location,
		Vector2 designPosition)
	{
		Location = location;
		DesignPosition = designPosition;
		Refresh();
	}

	private void Refresh()
	{
		if (Location is not { } location || !IsInsideTree())
		{
			return;
		}

		_nameLabel.Text = MapEntityPresentation.ResolveLocationName(location.Location);
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		_avatar.Texture = MapEntityPresentation.ResolveAvatarTexture(
			DefaultTexture,
			location.Location,
			location.Event);
	}
}
