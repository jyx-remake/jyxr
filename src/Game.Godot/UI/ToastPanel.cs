using Godot;

namespace Game.Godot.UI;

public partial class ToastPanel : Control
{
	private readonly Queue<string> _pendingMessages = [];
	private Label _messageLabel = null!;
	private bool _isPlaying;

	public override void _Ready()
	{
		_messageLabel = GetNode<Label>("%MessageLabel");
	}

	public void Enqueue(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		_pendingMessages.Enqueue(text);
		if (!_isPlaying)
		{
			_ = PlayQueueAsync();
		}
	}

	private async Task PlayQueueAsync()
	{
		_isPlaying = true;

		while (_pendingMessages.TryDequeue(out var message))
		{
			_messageLabel.Text = message;
			Modulate = new Color(1f, 1f, 1f, 0f);
			Show();

			await TweenModulateAsync(Colors.White, 0.16d);
			await ToSignal(GetTree().CreateTimer(1d), SceneTreeTimer.SignalName.Timeout);
			await TweenModulateAsync(new Color(1f, 1f, 1f, 0f), 0.24d);
			Hide();
		}

		_isPlaying = false;
	}

	private async Task TweenModulateAsync(Color target, double duration)
	{
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", target, duration);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

}
