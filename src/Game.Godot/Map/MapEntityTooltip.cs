using Godot;

namespace Game.Godot.Map;

public partial class MapEntityTooltip : PanelContainer
{
	private RichTextLabel _richTextLabel = null!;
	private string _text = string.Empty;
	
	public override void _Ready()
	{
		_richTextLabel = GetNode<RichTextLabel>("%RichTextLabel");
		Refresh();
	}

	public void Setup(string text)
	{
		_text = text;
		Refresh();
	}

	public static MapEntityTooltip Create(PackedScene scene, string text)
	{
		ArgumentNullException.ThrowIfNull(scene);
		if (scene.Instantiate() is not MapEntityTooltip tooltip)
		{
			throw new InvalidOperationException("Map entity tooltip scene root must be MapEntityTooltip.");
		}

		tooltip.Setup(text);
		return tooltip;
	}

	public static MapEntityTooltip Show(
		Control parent,
		PackedScene scene,
		string text,
		Rect2 anchor,
		Rect2 bounds)
	{
		var tooltip = Create(scene, text);
		IgnoreMouseInputRecursive(tooltip);
		parent.AddChild(tooltip);

		var size = tooltip.GetCombinedMinimumSize();
		var position = new Vector2(anchor.GetCenter().X - size.X * 0.5f, anchor.End.Y);
		if (position.Y + size.Y > bounds.End.Y)
		{
			position.Y = anchor.Position.Y - size.Y;
		}

		tooltip.Position = new Vector2(
			ClampAxis(position.X, size.X, bounds.Position.X, bounds.End.X),
			ClampAxis(position.Y, size.Y, bounds.Position.Y, bounds.End.Y));
		tooltip.CustomMinimumSize = size;
		tooltip.Size = size;
		tooltip.ZIndex = 1000;
		tooltip.ZAsRelative = false;
		return tooltip;
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		_richTextLabel.Text = _text;
	}

	private static float ClampAxis(float position, float length, float minimum, float maximum) =>
		length >= maximum - minimum
			? minimum
			: Mathf.Clamp(position, minimum, maximum - length);

	private static void IgnoreMouseInputRecursive(Control control)
	{
		control.MouseFilter = MouseFilterEnum.Ignore;
		foreach (var child in control.GetChildren())
		{
			if (child is Control childControl)
			{
				IgnoreMouseInputRecursive(childControl);
			}
		}
	}
}
