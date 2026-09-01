using Godot;

namespace Game.Godot.UI;

public partial class HintBox : Control
{
	private RichTextLabel _contentLabel = null!;
	private BaseButton _ackButton = null!;
	private Label _ackLabel = null!;
	private Label _titleLabel = null!;
	private TaskCompletionSource _completion = null!;

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_ackButton = GetNode<BaseButton>("%AckButton");
		_ackLabel = _ackButton.GetNode<Label>("Label");
		_titleLabel = GetNode<Label>("Panel/TitleLabel");
		_ackButton.Pressed += OnAckButtonPressed;
		Hide();
	}

	public Task ShowHintAsync(string text, CancellationToken cancellationToken = default) =>
		ShowHintAsync(text, "提示", "确认", cancellationToken);

	public async Task ShowHintAsync(
		string text,
		string title,
		string acknowledgeText,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(acknowledgeText);

		_completion?.TrySetResult();
		_completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_contentLabel.Text = text;
		_titleLabel.Text = title;
		_ackLabel.Text = acknowledgeText;
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
