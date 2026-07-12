using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class SelectHeadPanel : Control
{
	private readonly TaskCompletionSource<string> _completion = new();
	private SelectHeadSlot? _selectedSlot;
	private string _selectedHead = string.Empty;
	private GridContainer _headContainer = null!;
	private TextureButton _ackButton = null!;

	[Export]
	public PackedScene SelectHeadSlotScene { get; set; } = null!;

	public override void _Ready()
	{
		_headContainer = GetNode<GridContainer>("%HeadContainer");
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
		foreach (var child in _headContainer.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var head in Game.Config.SelectablePortraitIds)
		{
			if (SelectHeadSlotScene.Instantiate() is not SelectHeadSlot slot)
			{
				throw new InvalidOperationException("Select head slot scene root must be SelectHeadSlot.");
			}

			_headContainer.AddChild(slot);
			slot.SetTexture(AssetResolver.LoadTextureResource(head));
			slot.Pressed += () => SelectHead(slot, head);
		}
	}

	private void SelectHead(SelectHeadSlot slot, string head)
	{
		_selectedSlot?.SetSelected(false);
		_selectedSlot = slot;
		_selectedHead = head;
		slot.SetSelected(true);
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
