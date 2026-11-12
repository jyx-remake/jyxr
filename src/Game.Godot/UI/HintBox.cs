using Godot;

namespace Game.Godot.UI;

public partial class HintBox : Control, IPanelBackHandler
{
	private RichTextLabel _contentLabel = null!;
	private BaseButton _ackButton = null!;
	private TaskCompletionSource _completion = null!;

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_ackButton = GetNode<BaseButton>("%AckButton");
		_ackButton.Pressed += HandleBack;
		Hide();
	}

	public async Task ShowHintAsync(string text, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		_completion?.TrySetResult();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_completion = completion;
		_contentLabel.Text = text;
		Show();

		try
		{
			using var registration = cancellationToken.Register(static state =>
			{
				((TaskCompletionSource)state!).TrySetCanceled();
			}, completion);
			await completion.Task;
		}
		finally
		{
			if (ReferenceEquals(_completion, completion))
			{
				Hide();
				_completion = null!;
			}
		}
	}

	public void ResetImmediate()
	{
		var completion = _completion;
		_completion = null!;
		Hide();
		completion?.TrySetResult();
	}

	public void HandleBack()
	{
		_completion?.TrySetResult();
	}
}
