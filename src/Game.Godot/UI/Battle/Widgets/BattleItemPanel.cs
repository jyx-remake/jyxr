using Game.Core.Model;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleItemPanel : JyPanel
{
	private const float DesignWidth = 1920f;
	private const float DesignHeight = 1080f;
	private const float MinPanelScale = 0.62f;
	private const float MaxPanelScale = 1f;
	private const float MinItemScale = 0.58f;
	private const float MaxItemScale = 1f;
	private const float MinScreenMargin = 18f;
	private const int MinGridColumns = 2;
	private const int MaxGridColumns = 7;

	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private readonly TaskCompletionSource<InventoryEntry?> _selectionCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private GridContainer _gridContainer = null!;
	private Control _designCanvas = null!;
	private Control _designRoot = null!;
	private Label _titleLabel = null!;
	private Label _hintLabel = null!;
	private ScrollContainer _scrollContainer = null!;
	private Label _countLabel = null!;
	private Label _emptyLabel = null!;
	private IReadOnlyList<InventoryEntry> _entries = [];
	private float _itemScale = 1f;

	public override void _Ready()
	{
		base._Ready();
		_designCanvas = GetNode<Control>("DesignCanvas");
		_designRoot = GetNode<Control>("DesignCanvas/DesignRoot");
		_titleLabel = GetNode<Label>("DesignCanvas/DesignRoot/TitleLabel");
		_hintLabel = GetNode<Label>("DesignCanvas/DesignRoot/HintLabel");
		_scrollContainer = GetNode<ScrollContainer>("DesignCanvas/DesignRoot/ScrollContainer");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_countLabel = GetNode<Label>("%CountLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		ClosePanelRequested += () => _selectionCompletion.TrySetResult(null);
		ApplyResponsiveLayout();
		Refresh();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized)
		{
			ApplyResponsiveLayout();
		}
	}

	public void Configure(IReadOnlyList<InventoryEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);
		_entries = entries;
		Refresh();
	}

	public async Task<InventoryEntry?> AwaitSelectionAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_selectionCompletion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _selectionCompletion.Task;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!_selectionCompletion.Task.IsCompleted)
		{
			_selectionCompletion.TrySetResult(null);
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		ClearGrid();
		_countLabel.Text = $"{_entries.Count} 项";
		_emptyLabel.Visible = _entries.Count == 0;

		foreach (var entry in _entries)
		{
			_gridContainer.AddChild(CreateItemBox(entry));
		}
	}

	private void ApplyResponsiveLayout()
	{
		if (!IsNodeReady() || Size.X <= 0f || Size.Y <= 0f)
		{
			return;
		}

		FillRoot(_designCanvas);
		FillRoot(_designRoot);

		var scale = Mathf.Clamp(MathF.Min(Size.X / DesignWidth, Size.Y / DesignHeight), MinPanelScale, MaxPanelScale);
		var margin = MathF.Max(MinScreenMargin, 56f * scale);
		var contentWidth = MathF.Max(1f, Size.X - margin * 2f);
		var titleTop = MathF.Max(18f, 48f * scale);
		var titleHeight = 82f * scale;
		var hintTop = titleTop + titleHeight + 4f * scale;
		var hintHeight = 42f * scale;
		var countTop = hintTop + hintHeight + 8f * scale;
		var countHeight = 40f * scale;
		var scrollTop = countTop + countHeight + 12f * scale;
		var scrollBottom = Size.Y - MathF.Max(48f, 84f * scale);

		_titleLabel.Position = new Vector2(margin, titleTop);
		_titleLabel.Size = new Vector2(contentWidth, titleHeight);
		_hintLabel.Position = new Vector2(margin, hintTop);
		_hintLabel.Size = new Vector2(contentWidth, hintHeight);
		_countLabel.Position = new Vector2(margin, countTop);
		_countLabel.Size = new Vector2(contentWidth, countHeight);
		_scrollContainer.Position = new Vector2(margin, scrollTop);
		_scrollContainer.Size = new Vector2(contentWidth, MathF.Max(120f, scrollBottom - scrollTop));
		_emptyLabel.Position = new Vector2(margin, scrollTop + 120f * scale);
		_emptyLabel.Size = new Vector2(contentWidth, 58f * scale);

		var itemScaleByHeight = MathF.Min(MaxItemScale, (_scrollContainer.Size.Y - 16f * scale) / InventoryItemBox.DesignHeight);
		var minItemWidth = InventoryItemBox.DesignWidth * MinItemScale + 8f;
		var columns = Mathf.Clamp((int)MathF.Floor((contentWidth + 18f * scale) / minItemWidth), MinGridColumns, MaxGridColumns);
		var itemScaleByWidth = (contentWidth - (columns - 1) * 18f * scale) / (columns * InventoryItemBox.DesignWidth);
		_itemScale = Mathf.Clamp(MathF.Min(itemScaleByHeight, itemScaleByWidth), MinItemScale, MaxItemScale);
		_gridContainer.Columns = columns;
		_gridContainer.AddThemeConstantOverride("h_separation", Math.Max(6, (int)MathF.Round(18f * scale)));
		_gridContainer.AddThemeConstantOverride("v_separation", Math.Max(8, (int)MathF.Round(26f * scale)));
		ApplyItemScaleToGrid();
		ApplyLabelFontSizes(scale);
	}

	private void ApplyItemScaleToGrid()
	{
		if (_gridContainer is null)
		{
			return;
		}

		foreach (var child in _gridContainer.GetChildren())
		{
			if (child is InventoryItemBox itemBox)
			{
				itemBox.SetPresentationScale(_itemScale);
			}
		}
	}

	private void ApplyLabelFontSizes(float scale)
	{
		_titleLabel.AddThemeFontSizeOverride("font_size", Math.Max(32, (int)MathF.Round(58f * scale)));
		_titleLabel.AddThemeConstantOverride("outline_size", Math.Max(3, (int)MathF.Round(8f * scale)));
		_hintLabel.AddThemeFontSizeOverride("font_size", Math.Max(18, (int)MathF.Round(30f * scale)));
		_countLabel.AddThemeFontSizeOverride("font_size", Math.Max(16, (int)MathF.Round(28f * scale)));
		_emptyLabel.AddThemeFontSizeOverride("font_size", Math.Max(18, (int)MathF.Round(32f * scale)));
	}

	private InventoryItemBox CreateItemBox(InventoryEntry entry)
	{
		if (InventoryItemBoxScene is null)
		{
			throw new InvalidOperationException("InventoryItemBoxScene is not assigned.");
		}

		var instance = InventoryItemBoxScene.Instantiate();
		if (instance is not InventoryItemBox itemBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("InventoryItemBox scene root must be InventoryItemBox.");
		}

		itemBox.Setup(entry);
		itemBox.SetPresentationScale(_itemScale);
		itemBox.EntrySelected += OnEntrySelected;
		return itemBox;
	}

	private void OnEntrySelected(InventoryEntry entry)
	{
		if (_selectionCompletion.TrySetResult(entry))
		{
			QueueFree();
		}
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static void FillRoot(Control control)
	{
		control.Position = Vector2.Zero;
		control.Size = control.GetParent<Control>()?.Size ?? control.Size;
		control.Scale = Vector2.One;
	}
}
