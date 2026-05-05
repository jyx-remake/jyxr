using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Story;

public partial class StoryDialoguePanel : Control
{
	private static bool _skipMode;
	private static bool _isHighMode;
	private TaskCompletionSource<bool>? _completionSource;
	private string _speaker = string.Empty;
	private string _text = string.Empty;
	private Control _shadowPanel = null!;
	private AvatarBox _avatarBox = null!;
	private Label _speakerLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private Button _skipButton = null!;

	public int PresentationVersion { get; private set; }

	public override void _Ready()
	{
		_shadowPanel = GetNode<Control>("%ShadowPanel");
		_avatarBox = GetNode<AvatarBox>("%AvatarBox");
		_speakerLabel = GetNode<Label>("%SpeakerLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_skipButton = GetNode<Button>("%SkipButton");

		_contentLabel.GuiInput += OnContentLabelGuiInput;
		_shadowPanel.GuiInput += OnShadowPanelGuiInput;
		_skipButton.ButtonDown += OnSkipButtonDown;
		_skipButton.ButtonUp += OnSkipButtonUp;
		Apply();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || _completionSource is null || _completionSource.Task.IsCompleted)
		{
			return;
		}

		if (@event.IsActionPressed("ui-ctrl"))
		{
			_isHighMode = true;
			Complete();
			AcceptEvent();
			return;
		}

		if (@event.IsActionReleased("ui-ctrl"))
		{
			_isHighMode = false;
			AcceptEvent();
			return;
		}

		if (@event.IsActionPressed("ui_accept") ||
			@event.IsActionPressed("ui_select") ||
			@event.IsActionPressed("ui_text_submit"))
		{
			Complete();
			AcceptEvent();
		}
	}

	public void Configure(string? speaker, string? text)
	{
		_speaker = speaker?.Trim() ?? string.Empty;
		_text = text ?? string.Empty;
		_completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
		PresentationVersion += 1;

		if (IsInsideTree())
		{
			Apply();
		}

		Show();
	}

	public async Task AwaitCompletionAsync(CancellationToken cancellationToken = default)
	{
		if (_completionSource is null)
		{
			throw new InvalidOperationException("Dialogue panel must be configured before awaiting completion.");
		}

		using var registration = cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));
		if (_skipMode)
		{
			return;
		}

		if (_isHighMode)
		{
			await ToSignal(GetTree().CreateTimer(0.1d), SceneTreeTimer.SignalName.Timeout);
			return;
		}

		await _completionSource.Task;
	}

	private void Apply()
	{
		if (!IsInsideTree())
		{
			return;
		}

		var (displayName, portrait) = AssetResolver.ResolveSpeakerPresentation(_speaker);
		var hasSpeaker = !string.IsNullOrWhiteSpace(displayName);

		_avatarBox.Visible = portrait is not null;
		_avatarBox.SetAvatarTexture(portrait);
		_speakerLabel.Visible = hasSpeaker;
		_speakerLabel.Text = displayName;
		_contentLabel.Text = _text;
		_skipButton.Text = "跳过";
	}

	private void OnShadowPanelGuiInput(InputEvent @event)
	{
		if (!IsAdvanceClick(@event))
		{
			return;
		}

		Complete();
		AcceptEvent();
	}

	private void OnContentLabelGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			return;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		Complete();
		AcceptEvent();
	}

	private static bool IsAdvanceClick(InputEvent @event)
	{
		return @event is InputEventMouseButton
		{
			Pressed: true,
			ButtonIndex: MouseButton.Left
		};
	}

	private void Complete()
	{
		_completionSource?.TrySetResult(true);
	}

	private void OnSkipButtonDown()
	{
		_skipMode = true;
		Complete();
	}

	private static void OnSkipButtonUp()
	{
		_skipMode = false;
	}

	public void HidePanel() => Hide();
}
