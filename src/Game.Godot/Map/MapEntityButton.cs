using Game.Core.Definitions;
using Godot;

namespace Game.Godot.Map;

public partial class MapEntityButton : Button
{
	private static MapEntityButton? _activeMobileTooltipOwner;

	[Export]
	public Texture2D? DefaultTexture { get; set; }

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;
	private Control? _mobileTooltip;

	private (string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)? _location;

	public event Action<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)>? LocationPressed;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		Pressed += OnPressed;
		SetProcessInput(false);
		Refresh();
	}

	public override string _GetTooltip(Vector2 atPosition) => BuildTooltipText();

	public override Control? _MakeCustomTooltip(string forText) =>
		CreateTooltipView(forText);

	public string BuildTooltipText() =>
		_location is { } location ? MapEntityPresentation.BuildTooltipText(location) : string.Empty;

	public Control? CreateTooltipView(string text) =>
		string.IsNullOrWhiteSpace(text) ? null : MapEntityTooltip.Create(TooltipScene, text);

	public override void _ExitTree()
	{
		CloseMobileTooltip();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMobileTooltipArmed() || !IsMobileTooltipDismissInput(@event, out var position))
		{
			return;
		}

		if (!IsViewportPositionInside(position))
		{
			CloseMobileTooltip();
		}
	}

	private bool IsViewportPositionInside(Vector2 viewportPosition)
	{
		var localPosition = GetGlobalTransformWithCanvas().AffineInverse() * viewportPosition;
		return new Rect2(Vector2.Zero, Size).HasPoint(localPosition);
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

		_nameLabel.Text = MapEntityPresentation.ResolveLocationName(location.Location);
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		_avatar.Texture = MapEntityPresentation.ResolveAvatarTexture(
			DefaultTexture,
			location.Location,
			location.Event);
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

		if (!Game.IsMobilePlatform)
		{
			LocationPressed?.Invoke(location);
			return;
		}

		var tooltipText = MapEntityPresentation.BuildTooltipText(location);
		if (string.IsNullOrWhiteSpace(tooltipText))
		{
			LocationPressed?.Invoke(location);
			return;
		}

		if (!IsMobileTooltipArmed())
		{
			ShowMobileTooltip(tooltipText);
			return;
		}

		CloseMobileTooltip();
		LocationPressed?.Invoke(location);
	}

	private void ShowMobileTooltip(string text)
	{
		if (_activeMobileTooltipOwner is not null &&
			_activeMobileTooltipOwner != this &&
			GodotObject.IsInstanceValid(_activeMobileTooltipOwner))
		{
			_activeMobileTooltipOwner.CloseMobileTooltip();
		}

		CloseMobileTooltip();

		_mobileTooltip = MapEntityTooltip.Show(
			this,
			TooltipScene,
			text,
			new Rect2(Vector2.Zero, Size),
			GetLocalViewportRect());
		_activeMobileTooltipOwner = this;
		SetProcessInput(true);
	}

	private Rect2 GetLocalViewportRect()
	{
		var viewportRect = GetViewportRect();
		var viewportToLocal = GetGlobalTransformWithCanvas().AffineInverse();
		var firstCorner = viewportToLocal * viewportRect.Position;
		var secondCorner = viewportToLocal * viewportRect.End;
		return new Rect2(firstCorner, secondCorner - firstCorner).Abs();
	}

	private bool IsMobileTooltipArmed() =>
		_activeMobileTooltipOwner == this &&
		_mobileTooltip is not null &&
		GodotObject.IsInstanceValid(_mobileTooltip);

	private void CloseMobileTooltip()
	{
		if (_activeMobileTooltipOwner == this)
		{
			_activeMobileTooltipOwner = null;
		}

		var tooltip = _mobileTooltip;
		_mobileTooltip = null;
		SetProcessInput(false);
		if (tooltip is null || !GodotObject.IsInstanceValid(tooltip))
		{
			return;
		}

		tooltip.QueueFree();
	}

	private static bool IsMobileTooltipDismissInput(InputEvent @event, out Vector2 position)
	{
		switch (@event)
		{
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton:
				position = mouseButton.Position;
				return true;
			default:
				position = default;
				return false;
		}
	}

}
