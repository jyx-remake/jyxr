using Godot;

namespace Game.Godot.UI;

public partial class SelectHeadPanel : Control
{
	private readonly TaskCompletionSource<string> _completion = new();
	private string _selectedHead = string.Empty;
	private VirtualGrid _headContainer = null!;
	private TextureButton _ackButton = null!;

	[Export]
	public PackedScene SelectHeadSlotScene { get; set; } = null!;

	public override void _Ready()
	{
		_headContainer = GetNode<VirtualGrid>("%HeadContainer");
		_ackButton = GetNode<TextureButton>("%AckButton");
		_ackButton.Pressed += Submit;
		FillHeads();
	}

	public async Task<string> AwaitHeadAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_completion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _completion.Task;
	}

	public override void _ExitTree()
	{
		if (!_completion.Task.IsCompleted)
		{
			_completion.TrySetCanceled();
		}
	}

	private void FillHeads()
	{
		_headContainer.SetItems(Game.Config.SelectablePortraitIds, CreateHeadSlot,
			(slot, head, _) => slot.Setup(head, string.Equals(head, _selectedHead, StringComparison.Ordinal)));
	}

	private SelectHeadSlot CreateHeadSlot()
	{
		if (SelectHeadSlotScene.Instantiate() is not SelectHeadSlot slot)
		{
			throw new InvalidOperationException("Select head slot scene root must be SelectHeadSlot.");
		}

		slot.Pressed += () => SelectHead(slot.PortraitId);
		return slot;
	}

	private void SelectHead(string head)
	{
		_selectedHead = head;
		_headContainer.RefreshItems();
	}

	private void Submit()
	{
		if (string.IsNullOrWhiteSpace(_selectedHead))
		{
			return;
		}

		if (_completion.TrySetResult(_selectedHead))
		{
			QueueFree();
		}
	}
}
