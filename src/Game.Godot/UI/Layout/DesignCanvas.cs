using Godot;

namespace Game.Godot.UI.Layout;

public partial class DesignCanvas : Control
{
	[Export]
	public Vector2 DesignSize { get; set; } = new(1920f, 1080f);

	[Export]
	public NodePath DesignRootPath { get; set; } = new("DesignRoot");

	private Control? _designRoot;

	public override void _Ready()
	{
		_designRoot = GetNodeOrNull<Control>(DesignRootPath);
		ApplyLayout();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);

		if (what == NotificationResized)
		{
			ApplyLayout();
		}
	}

	private void ApplyLayout()
	{
		if (_designRoot is null || DesignSize.X <= 0f || DesignSize.Y <= 0f || Size.X <= 0f || Size.Y <= 0f)
		{
			return;
		}

		var scale = Mathf.Min(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
		var scaledSize = DesignSize * scale;
		_designRoot.Position = (Size - scaledSize) * 0.5f;
		_designRoot.Size = DesignSize;
		_designRoot.Scale = new Vector2(scale, scale);
	}
}
