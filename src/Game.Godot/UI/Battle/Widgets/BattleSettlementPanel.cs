using Game.Core.Model;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSettlementPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private readonly TaskCompletionSource _confirmationCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private Label _titleLabel = null!;
	private Label _detailLabel = null!;
	private Label _rewardHeaderLabel = null!;
	private Label _countLabel = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private TextureButton _confirmButton = null!;
	private Label _confirmButtonLabel = null!;
	private BattleSettlementView? _view;
	private Control? _rewardDetailPanel;

	public override void _Ready()
	{
		base._Ready();
		CloseButton.Hide();
		_titleLabel = GetNode<Label>("%TitleLabel");
		_detailLabel = GetNode<Label>("%DetailLabel");
		_rewardHeaderLabel = GetNode<Label>("%RewardHeaderLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_confirmButton = GetNode<TextureButton>("%ConfirmButton");
		_confirmButtonLabel = GetNode<Label>("%ConfirmButtonLabel");
		_confirmButton.Pressed += Confirm;
		ClosePanelRequested += Confirm;
		Refresh();
	}

	public void Configure(BattleSettlementView view)
	{
		ArgumentNullException.ThrowIfNull(view);
		_view = view;
		Refresh();
	}

	public async Task AwaitConfirmationAsync(
		double autoConfirmDelaySeconds = 0d,
		CancellationToken cancellationToken = default)
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

		if (autoConfirmDelaySeconds > 0d)
		{
			_ = ConfirmAfterDelayAsync(autoConfirmDelaySeconds, cancellationToken);
		}

		await _confirmationCompletion.Task;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CloseRewardDetailPanel();
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

	private async Task ConfirmAfterDelayAsync(double seconds, CancellationToken cancellationToken)
	{
		var timer = GetTree().CreateTimer(seconds);
		await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
		if (!cancellationToken.IsCancellationRequested && GodotObject.IsInstanceValid(this))
		{
			Confirm();
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
		itemBox.EntrySelected += ShowRewardDetail;
		return itemBox;
	}

	private void ShowRewardDetail(InventoryEntry entry)
	{
		var detailPanel = UIRoot.Instance.ShowInventoryEntryDetailPanel(entry);
		detailPanel.ZIndex = 1000;
		detailPanel.ZAsRelative = false;
		_rewardDetailPanel = detailPanel;
		detailPanel.TreeExited += () => ClearRewardDetailPanelReference(detailPanel);
	}

	private void CloseRewardDetailPanel()
	{
		if (_rewardDetailPanel is not null && GodotObject.IsInstanceValid(_rewardDetailPanel))
		{
			_rewardDetailPanel.QueueFree();
		}

		_rewardDetailPanel = null;
	}

	private void ClearRewardDetailPanelReference(Control panel)
	{
		if (ReferenceEquals(_rewardDetailPanel, panel))
		{
			_rewardDetailPanel = null;
		}
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}
