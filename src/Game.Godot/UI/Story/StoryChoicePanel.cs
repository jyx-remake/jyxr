using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Story;

public partial class StoryChoicePanel : Control
{
	private TaskCompletionSource<int>? _completionSource;
	private string _speaker = string.Empty;
	private string _text = string.Empty;
	private IReadOnlyList<string> _options = [];
	private AvatarBox _avatarBox = null!;
	private RichTextLabel _questionLabel = null!;
	private VBoxContainer _choiceContainer = null!;

	[Export]
	public PackedScene ChoiceButtonScene { get; set; } = null!;

	public override void _Ready()
	{
		_avatarBox = GetNode<AvatarBox>("%AvatarBox");
		_questionLabel = GetNode<RichTextLabel>("%QuestionLabel");
		_choiceContainer = GetNode<VBoxContainer>("%ChoiceContainer");
		Apply();
	}

	public void Configure(string? speaker, string? text, IReadOnlyList<string> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.Count == 0)
		{
			throw new InvalidOperationException("Choice panel requires at least one option.");
		}

		_speaker = speaker?.Trim() ?? string.Empty;
		_text = text ?? string.Empty;
		_options = options;
		_completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		if (IsInsideTree())
		{
			Apply();
		}

		Show();
	}

	public async Task<int> AwaitSelectionAsync(CancellationToken cancellationToken = default)
	{
		if (_completionSource is null)
		{
			throw new InvalidOperationException("Choice panel must be configured before awaiting selection.");
		}

		using var registration = cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));
		return await _completionSource.Task;
	}

	private void Apply()
	{
		if (!IsInsideTree())
		{
			return;
		}

		var (_, portrait) = AssetResolver.ResolveSpeakerPresentation(_speaker);
		_avatarBox.Visible = portrait is not null;
		_avatarBox.SetAvatarTexture(portrait);
		_questionLabel.Text = _text;

		foreach (var child in _choiceContainer.GetChildren())
		{
			child.QueueFree();
		}

		StoryChoiceButton? firstButton = null;
		for (var index = 0; index < _options.Count; index += 1)
		{
			var button = CreateButton(_options[index], index);
			firstButton ??= button;
			_choiceContainer.AddChild(button);
		}

		firstButton?.GrabFocus();
	}

	private StoryChoiceButton CreateButton(string text, int index)
	{
		if (ChoiceButtonScene is null)
		{
			throw new InvalidOperationException("ChoiceButtonScene is not assigned.");
		}

		var instance = ChoiceButtonScene.Instantiate();
		if (instance is not StoryChoiceButton button)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Choice button scene root must be StoryChoiceButton.");
		}

		button.Configure(text);
		button.Pressed += () => Complete(index);
		return button;
	}

	private void Complete(int index)
	{
		foreach (var child in _choiceContainer.GetChildren())
		{
			if (child is BaseButton button)
			{
				button.Disabled = true;
			}
		}

		_completionSource?.TrySetResult(index);
	}

	public void HidePanel() => Hide();
}
