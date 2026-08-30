using Game.Core.Definitions;
using Godot;

namespace Game.Godot.Map;

public partial class MapEntityButton : Button
{
	private static readonly Vector2 DefaultAvatarPosition = new(39f, 42f);
	private static readonly Vector2 DefaultAvatarSize = new(172f, 172f);
	private static readonly Vector2 DefaultLabelPosition = new(-37f, 160f);
	private static readonly Vector2 DefaultLabelSize = new(325f, 164f);

	[Export]
	public Texture2D? DefaultTexture { get; set; }

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;
	private Material? _defaultAvatarMaterial;
	private (string MapId, MapLocationDefinition Location, MapEventDefinition? Event)? _location;

	public event Action<
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event),
		Rect2>? LocationPressed;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_defaultAvatarMaterial = _avatar.Material;
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		Pressed += OnPressed;
		Refresh();
	}

	public override string _GetTooltip(Vector2 atPosition) => BuildTooltipText();

	public override Control? _MakeCustomTooltip(string forText) =>
		CreateTooltipView(forText);

	public string BuildTooltipText() =>
		_location is { } location ? MapEntityPresentation.BuildTooltipText(location) : string.Empty;

	public Control? CreateTooltipView(string text) =>
		string.IsNullOrWhiteSpace(text) ? null : MapEntityTooltip.Create(TooltipScene, text);

	public void Setup((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
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

		_nameLabel.Text = MapEntityPresentation.ResolveLocationName(location.Location);
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		var avatar = MapEntityPresentation.ResolveAvatar(
			DefaultTexture,
			location.Location,
			location.Event);
		_avatar.Texture = avatar.Texture;
		ApplyAvatarLayout(avatar);
	}

	private void ApplyAvatarLayout(MapEntityAvatarPresentation avatar)
	{
		if (!avatar.UseNativeSize || avatar.Texture is null)
		{
			_avatar.Position = DefaultAvatarPosition;
			_avatar.Size = DefaultAvatarSize;
			_avatar.Material = _defaultAvatarMaterial;
			_nameLabel.Position = DefaultLabelPosition;
			_nameLabel.Size = DefaultLabelSize;
			return;
		}

		var textureSize = avatar.Texture.GetSize();
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			return;
		}

		// Match the legacy launcher: town.native.* and town.city.* use the
		// source texture dimensions and are anchored at the button's bottom
		// centre instead of being forced into the 172x172 masked thumbnail.
		_avatar.Size = textureSize;
		_avatar.Position = new Vector2((256f - textureSize.X) * 0.5f, 214f - textureSize.Y);
		// Native-size town art is displayed without the small-map portrait mask.
		// No outline is applied here; map-node outlining belongs to LargeMapMarker.
		_avatar.Material = null;
	}

	private void OnPressed()
	{
		Activate();
	}

	public void Activate()
	{
		if (_location is not { } location)
		{
			return;
		}

		if (location.Event is null)
		{
			return;
		}

		LocationPressed?.Invoke(location, GetGlobalRect());
	}
}
