using Game.Godot.Resources;
using Godot;

namespace Game.Godot.UI.Story;

public partial class StoryDialoguePanel : Control
{
	private const double TypewriterCharactersPerSecond = 36d;
	private TaskCompletionSource<bool>? _completionSource;
	private string _speaker = string.Empty;
	private string _text = string.Empty;
	private AvatarBox _avatarBox = null!;
	private Label _speakerLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private Control _contentViewport = null!;
	private readonly List<DialoguePage> _pages = [];
	private int _pageIndex;
	private Button _skipButton = null!;
	private bool _isTyping;
	private bool _isSkipping;
	private double _typewriterProgress;
	private int _typewriterTargetCharacters;
	private sealed record DialoguePage(float Offset, int Start, int End);

	public int PresentationVersion { get; private set; }

	public override void _Ready()
	{
		_avatarBox = GetNode<AvatarBox>("%AvatarBox");
		_speakerLabel = GetNode<Label>("%SpeakerLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_contentViewport = GetNode<Control>("%ContentViewport");
		_skipButton = GetNode<Button>("%SkipButton");

		_skipButton.Pressed += RequestSkip;
		SetProcess(false);
		Apply();
	}

	public override void _Process(double delta)
	{
		if (!_isTyping)
		{
			return;
		}

		_typewriterProgress += delta * TypewriterCharactersPerSecond;
		var visibleCharacters = Math.Min(
			_typewriterTargetCharacters,
			Math.Max(1, (int)Math.Floor(_typewriterProgress)));
		_contentLabel.VisibleCharacters = _pages[_pageIndex].Start + visibleCharacters;

		if (visibleCharacters >= _typewriterTargetCharacters)
		{
			RevealCurrentPage();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		OnAdvanceGuiInput(@event);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || _completionSource is null || _completionSource.Task.IsCompleted)
		{
			return;
		}

		if (@event.IsActionPressed("ui-ctrl"))
		{
			Complete();
			AcceptEvent();
			return;
		}

		if (@event.IsActionPressed("ui_accept") ||
			@event.IsActionPressed("ui_select") ||
			@event.IsActionPressed("ui_text_submit"))
		{
			Advance();
			AcceptEvent();
		}
	}

	public void Configure(string? speaker, string? text)
	{
		_speaker = speaker?.Trim() ?? string.Empty;
		_text = text ?? string.Empty;
		_completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
		PresentationVersion += 1;

		if (_isSkipping)
		{
			_completionSource.TrySetResult(true);
			Hide();
			return;
		}

		if (IsInsideTree())
		{
			Apply();
		}

		Show();
	}

	public async Task AwaitCompletionAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_completionSource is null)
		{
			throw new InvalidOperationException("Dialogue panel must be configured before awaiting completion.");
		}

		using var registration = cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));
		if (!_completionSource.Task.IsCompleted && Input.IsActionPressed("ui-ctrl"))
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
		BuildPages();
		ShowPage(0);
	}

	private void BuildPages()
	{
		_pages.Clear();
		_contentLabel.VisibleCharacters = -1;
		_contentLabel.Position = Vector2.Zero;
		_contentLabel.Size = _contentViewport.Size;
		var lineCount = _contentLabel.GetLineCount();
		var characterCount = _contentLabel.GetTotalCharacterCount();
		var firstLine = 0;
		while (firstLine < lineCount)
		{
			var offset = _contentLabel.GetLineOffset(firstLine);
			var nextLine = firstLine + 1;
			while (nextLine < lineCount &&
				_contentLabel.GetLineOffset(nextLine) - offset + _contentLabel.GetLineHeight(nextLine)
				<= _contentViewport.Size.Y)
			{
				nextLine++;
			}

			var start = firstLine == 0 ? 0 : _contentLabel.GetLineRange(firstLine).X;
			var end = nextLine < lineCount ? _contentLabel.GetLineRange(nextLine).X : characterCount;
			_pages.Add(new DialoguePage(offset, start, end));
			firstLine = nextLine;
		}

		if (_pages.Count == 0)
		{
			_pages.Add(new DialoguePage(0, 0, 0));
		}

		// Keep one shaped document so BBCode spans and wrapping survive page changes.
		_contentLabel.Size = new Vector2(_contentViewport.Size.X,
			Math.Max(_contentViewport.Size.Y, _contentLabel.GetContentHeight()));
	}

	private void ShowPage(int index)
	{
		_pageIndex = index;
		_contentLabel.Position = new Vector2(0, -_pages[index].Offset);

		if (_completionSource is null || _pages[index].End == _pages[index].Start)
		{
			RevealCurrentPage();
			return;
		}

		if (global::Game.Godot.Game.Settings.DialogueTypewriterEnabled)
		{
			StartTypewriter();
			return;
		}

		RevealCurrentPage();
	}

	private void OnAdvanceGuiInput(InputEvent @event)
	{
		if (!IsAdvanceInput(@event))
		{
			return;
		}

		Advance();
		AcceptEvent();
	}

	private static bool IsAdvanceInput(InputEvent @event) =>
		@event is InputEventMouseButton
		{
			Pressed: true,
			ButtonIndex: MouseButton.Left
		};

	private void StartTypewriter()
	{
		_typewriterProgress = 0d;
		_typewriterTargetCharacters = _pages[_pageIndex].End - _pages[_pageIndex].Start;
		_contentLabel.VisibleCharacters = _pages[_pageIndex].Start;
		_isTyping = true;
		SetProcess(true);
	}

	private void RevealCurrentPage()
	{
		_isTyping = false;
		SetProcess(false);
		_contentLabel.VisibleCharacters = _pages.Count == 0 ? 0 : _pages[_pageIndex].End;
	}

	private void Advance()
	{
		if (!Visible || _completionSource is null || _completionSource.Task.IsCompleted)
		{
			return;
		}

		if (_isTyping)
		{
			RevealCurrentPage();
			return;
		}

		if (_pageIndex + 1 < _pages.Count)
		{
			ShowPage(_pageIndex + 1);
			return;
		}

		Complete();
	}

	private void RequestSkip()
	{
		_isSkipping = true;
		Complete();
	}

	public void StopSkipping() => _isSkipping = false;

	private void Complete()
	{
		RevealCurrentPage();
		_completionSource?.TrySetResult(true);
	}

	public void HidePanel()
	{
		RevealCurrentPage();
		Hide();
	}
}
