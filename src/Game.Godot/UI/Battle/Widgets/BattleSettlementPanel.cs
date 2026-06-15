using Game.Core.Model;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSettlementPanel : JyPanel
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

	private readonly TaskCompletionSource _confirmationCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private Control _designCanvas = null!;
	private Control _designRoot = null!;
	private Label _titleLabel = null!;
	private Label _detailLabel = null!;
	private Label _rewardHeaderLabel = null!;
	private Label _countLabel = null!;
	private ScrollContainer _scrollContainer = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private TextureButton _confirmButton = null!;
	private Label _confirmButtonLabel = null!;
	private BattleSettlementView? _view;
	private float _itemScale = 1f;

	public override void _Ready()
	{
		base._Ready();
		CloseButton.Hide();
		_designCanvas = GetNode<Control>("DesignCanvas");
		_designRoot = GetNode<Control>("DesignCanvas/DesignRoot");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_detailLabel = GetNode<Label>("%DetailLabel");
		_rewardHeaderLabel = GetNode<Label>("%RewardHeaderLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_scrollContainer = GetNode<ScrollContainer>("DesignCanvas/DesignRoot/ScrollContainer");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_confirmButton = GetNode<TextureButton>("%ConfirmButton");
		_confirmButtonLabel = GetNode<Label>("%ConfirmButtonLabel");
		_confirmButton.Pressed += Confirm;
		ClosePanelRequested += Confirm;
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

	public void Configure(BattleSettlementView view)
	{
		ArgumentNullException.ThrowIfNull(view);
		_view = view;
		Refresh();
	}

	public async Task AwaitConfirmationAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_confirmationCompletion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		await _confirmationCompletion.Task;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!_confirmationCompletion.Task.IsCompleted)
		{
			_confirmationCompletion.TrySetResult();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel"))
		{
			Confirm();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Confirm()
	{
		if (_confirmationCompletion.TrySetResult())
		{
			QueueFree();
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _view is null)
		{
			return;
		}

		_titleLabel.Text = _view.Title;
		_detailLabel.Text = _view.Detail;
		_detailLabel.Visible = !string.IsNullOrWhiteSpace(_view.Detail);
		_rewardHeaderLabel.Text = _view.RewardHeader;
		_rewardHeaderLabel.Visible = !string.IsNullOrWhiteSpace(_view.RewardHeader);
		_countLabel.Text = $"{_view.RewardEntries.Count} 项";
		_countLabel.Visible = _view.RewardEntries.Count > 0;
		_confirmButtonLabel.Text = _view.ConfirmText;
		FillRewardList(_view.RewardEntries);
	}

	private void FillRewardList(IReadOnlyList<InventoryEntry> rewardEntries)
	{
		ClearGrid();
		_emptyLabel.Visible = rewardEntries.Count == 0
			&& !string.IsNullOrWhiteSpace(_view?.RewardHeader);

		foreach (var rewardEntry in rewardEntries)
		{
			_gridContainer.AddChild(CreateItemBox(rewardEntry));
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
		var detailTop = titleTop + titleHeight + 4f * scale;
		var detailHeight = 64f * scale;
		var rewardTop = detailTop + detailHeight + 12f * scale;
		var rewardHeight = 44f * scale;
		var buttonSize = new Vector2(300f, 104f) * scale;
		var buttonTop = Size.Y - buttonSize.Y - MathF.Max(28f, 54f * scale);
		var scrollTop = rewardTop + rewardHeight + 10f * scale;
		var scrollBottom = buttonTop - 18f * scale;

		_titleLabel.Position = new Vector2(margin, titleTop);
		_titleLabel.Size = new Vector2(contentWidth, titleHeight);
		_detailLabel.Position = new Vector2(margin, detailTop);
		_detailLabel.Size = new Vector2(contentWidth, detailHeight);
		_rewardHeaderLabel.Position = new Vector2(margin, rewardTop);
		_rewardHeaderLabel.Size = new Vector2(contentWidth * 0.5f, rewardHeight);
		_countLabel.Position = new Vector2(margin + contentWidth * 0.5f, rewardTop);
		_countLabel.Size = new Vector2(contentWidth * 0.5f, rewardHeight);
		_scrollContainer.Position = new Vector2(margin, scrollTop);
		_scrollContainer.Size = new Vector2(contentWidth, MathF.Max(110f, scrollBottom - scrollTop));
		_emptyLabel.Position = new Vector2(margin, scrollTop + 120f * scale);
		_emptyLabel.Size = new Vector2(contentWidth, 58f * scale);
		_confirmButton.Position = new Vector2((Size.X - buttonSize.X) * 0.5f, buttonTop);
		_confirmButton.Size = new Vector2(300f, 104f);
		_confirmButton.Scale = Vector2.One * scale;

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
		_detailLabel.AddThemeFontSizeOverride("font_size", Math.Max(20, (int)MathF.Round(34f * scale)));
		_rewardHeaderLabel.AddThemeFontSizeOverride("font_size", Math.Max(20, (int)MathF.Round(34f * scale)));
		_countLabel.AddThemeFontSizeOverride("font_size", Math.Max(16, (int)MathF.Round(28f * scale)));
		_emptyLabel.AddThemeFontSizeOverride("font_size", Math.Max(18, (int)MathF.Round(32f * scale)));
		_confirmButtonLabel.AddThemeFontSizeOverride("font_size", Math.Max(24, (int)MathF.Round(44f * scale)));
		_confirmButtonLabel.AddThemeConstantOverride("outline_size", Math.Max(2, (int)MathF.Round(5f * scale)));
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
		return itemBox;
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
