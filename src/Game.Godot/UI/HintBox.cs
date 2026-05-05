using Godot;

namespace Game.Godot.UI;

public partial class HintBox : Control
{
	private RichTextLabel _contentLabel = null!;
	private BaseButton _ackButton = null!;
	private TaskCompletionSource _completion = null!;

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_ackButton = GetNode<BaseButton>("%AckButton");
		_ackButton.Pressed += OnAckButtonPressed;
		Hide();
	}

	public async Task ShowHintAsync(string text, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		_completion?.TrySetResult();
		_completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_contentLabel.Text = text;
		Show();

		try
		{
			using var registration = cancellationToken.Register(static state =>
			{
				((TaskCompletionSource)state!).TrySetCanceled();
			}, _completion);
			await _completion.Task;
		}
		finally
		{
			Hide();
			_completion = null!;
		}
	}

	private void OnAckButtonPressed()
	{
		_completion?.TrySetResult();
	}
}
