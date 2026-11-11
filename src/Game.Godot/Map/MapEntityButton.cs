using Game.Core.Definitions;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class MapEntityButton : Button
{
	[Export]
	public Texture2D? DefaultTexture { get; set; }

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private AutoFitRichTextLabel _nameLabel = null!;
	private TextureRect _notice = null!;
	private (string MapId, MapLocationDefinition Location, MapEventDefinition? Event)? _location;

	public event Action<
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event),
		Rect2>? LocationPressed;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<AutoFitRichTextLabel>("%NameLabel");
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

		_nameLabel.SetContent(MapEntityPresentation.ResolveLocationName(location.Location));
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		_avatar.Texture = MapEntityPresentation.ResolveAvatar(
			DefaultTexture,
			location.Location,
			location.Event).Texture;
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
