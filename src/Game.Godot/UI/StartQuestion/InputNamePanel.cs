using Godot;

namespace Game.Godot.UI;

public partial class InputNamePanel : Control
{
	private readonly TaskCompletionSource<string> _completion = new();
	private Label _hintLabel = null!;
	private LineEdit _nameEdit = null!;
	private TextureButton _ackButton = null!;

	public override void _Ready()
	{
		_hintLabel = GetNode<Label>("%HintLabel");
		_nameEdit = GetNode<LineEdit>("%NameEdit");
		_ackButton = GetNode<TextureButton>("%AckButton");
		_ackButton.Pressed += Submit;
		_nameEdit.TextSubmitted += _ => Submit();
	}

	public async Task<string> AwaitNameAsync(
		string characterId,
		string defaultName = "",
		CancellationToken cancellationToken = default)
	{
		_hintLabel.Text = $"请输入{characterId}名称";
		_nameEdit.Text = defaultName;
		_nameEdit.GrabFocus();

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

	private void Submit()
	{
		if (_completion.TrySetResult(_nameEdit.Text))
		{
			QueueFree();
		}
	}
}
