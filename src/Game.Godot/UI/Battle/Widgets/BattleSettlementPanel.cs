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
		return itemBox;
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}
