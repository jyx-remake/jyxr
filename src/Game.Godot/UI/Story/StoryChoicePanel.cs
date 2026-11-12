using Game.Godot.Resources;
using Godot;

namespace Game.Godot.UI.Story;

public partial class StoryChoicePanel : Control, IPanelBackHandler
{
	private TaskCompletionSource<int?>? _completionSource;
	private string _speaker = string.Empty;
	private string _text = string.Empty;
	private IReadOnlyList<string> _options = [];
	private AvatarBox _avatarBox = null!;
	private RichTextLabel _questionLabel = null!;
	private VBoxContainer _choiceContainer = null!;

	[Export]
	public PackedScene ChoiceButtonScene { get; set; } = null!;

	public bool AllowCancel { get; private set; }

	public override void _Ready()
	{
		_avatarBox = GetNode<AvatarBox>("%AvatarBox");
		_questionLabel = GetNode<RichTextLabel>("%QuestionLabel");
		_choiceContainer = GetNode<VBoxContainer>("%ChoiceContainer");
		Apply();
	}

	public async Task<int?> ShowAsync(
		string? speaker,
		string? text,
		IReadOnlyList<string> options,
		bool allowCancel = false,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.Count == 0 && !allowCancel)
		{
			throw new InvalidOperationException("Choice panel requires at least one option.");
		}

		_completionSource?.TrySetCanceled();
		var completion = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_completionSource = completion;
		AllowCancel = allowCancel;
		_speaker = speaker?.Trim() ?? string.Empty;
		_text = text ?? string.Empty;
		_options = options;

		try
		{
			if (IsInsideTree())
			{
				Apply();
			}

			Show();
			using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
			return await completion.Task;
		}
		finally
		{
			if (ReferenceEquals(_completionSource, completion))
			{
				HidePanel();
			}
		}
	}

	public void HandleBack()
	{
		if (AllowCancel)
		{
			Complete(_completionSource, null);
		}
	}

	public override void _ExitTree()
	{
		_completionSource?.TrySetCanceled();
		_completionSource = null;
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
		var completion = _completionSource;
		button.Pressed += () => Complete(completion, index);
		return button;
	}

	private void Complete(TaskCompletionSource<int?>? completion, int? index)
	{
		if (completion is null || !ReferenceEquals(_completionSource, completion) ||
			!completion.TrySetResult(index))
		{
			return;
		}

		foreach (var child in _choiceContainer.GetChildren())
		{
			if (child is BaseButton button)
			{
				button.Disabled = true;
			}
		}
	}

	public void HidePanel()
	{
		_completionSource?.TrySetCanceled();
		_completionSource = null;
		AllowCancel = false;
		Hide();
		_avatarBox.SetAvatarTexture(null);
	}
}
