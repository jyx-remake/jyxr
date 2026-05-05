using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

public partial class MapEntityButton : Button
{
	[Export]
	public Texture2D? DefaultTexture { get; set; }

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;

	private (string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)? _location;

	public event Action<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)>? LocationPressed;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		Pressed += OnPressed;
		Refresh();
	}

	public void Setup((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
	{
		_location = location;
		Refresh();
	}

	private void Refresh()
	{
		if (_location is not { } location || !IsInsideTree())
		{
			return;
		}
		_nameLabel.Text = location.Location.Name ?? AssetResolver.ResolveCharacterName(location.Location.Id);

		TooltipText = !string.IsNullOrWhiteSpace(location.Event?.Description)
			? location.Event.Description
			: location.Location.Description ?? "";
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;

		_avatar.Texture = ResolveAvatarTexture(location.Location,location.Event);
	}

	private Texture2D? ResolveAvatarTexture(MapLocationDefinition location, MapEventDefinition? mapEvent)
	{
		if (mapEvent is null)
		{
			return DefaultTexture;
		}

		var image = mapEvent.Image ?? location.Picture;
		if (image is not null)
		{
			return AssetResolver.LoadTextureResource(image) ?? DefaultTexture;
		}

		return AssetResolver.LoadCharacterPortraitByCharacterId(location.Id) ?? DefaultTexture;
	}

	private void OnPressed()
	{
		if (_location is { Event: not null } location)
		{
			LocationPressed?.Invoke(location);
		}
	}
}
