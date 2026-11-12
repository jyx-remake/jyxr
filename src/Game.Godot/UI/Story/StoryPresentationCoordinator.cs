using Game.Core.Story;
using Godot;

namespace Game.Godot.UI.Story;

internal sealed class StoryPresentationCoordinator(
	StoryDialoguePanel dialogue,
	StoryChoicePanel regularChoices,
	StoryChoicePanel boldChoices,
	StoryIntertitlePanel intertitle,
	StoryVideoPlayer video,
	StoryVisualEffects effects)
{
	public Task ShowDialogueAsync(string speaker, string text, CancellationToken cancellationToken = default) =>
		dialogue.ShowAsync(speaker, text, cancellationToken);

	public Task ShowIntertitleAsync(
		string text,
		string position = "center",
		string mode = "typewriter",
		double speed = 36d,
		CancellationToken cancellationToken = default)
	{
		dialogue.HidePanel();
		regularChoices.HidePanel();
		boldChoices.HidePanel();
		return intertitle.ShowAsync(text, position, mode, speed, cancellationToken);
	}

	public Task ShowVideoAsync(VideoStream stream, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(stream);
		dialogue.HidePanel();
		regularChoices.HidePanel();
		boldChoices.HidePanel();
		intertitle.ResetImmediate();
		return video.PlayAsync(stream, cancellationToken);
	}

	public async Task<int> ShowChoicesAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		ChoiceStyle style = ChoiceStyle.Regular,
		CancellationToken cancellationToken = default)
	{
		return await ShowChoicesCoreAsync(speaker, prompt, options, style, false, cancellationToken)
			?? throw new InvalidOperationException("A required choice must return a selection.");
	}

	public Task<int?> ShowCancelableChoicesAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		ChoiceStyle style = ChoiceStyle.Regular,
		CancellationToken cancellationToken = default) =>
		ShowChoicesCoreAsync(speaker, prompt, options, style, true, cancellationToken);

	private Task<int?> ShowChoicesCoreAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		ChoiceStyle style,
		bool allowCancel,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.Count == 0 && !allowCancel)
		{
			throw new InvalidOperationException("Choices cannot be empty.");
		}

		dialogue.StopSkipping();
		dialogue.HidePanel();
		regularChoices.HidePanel();
		boldChoices.HidePanel();

		var dialog = GetStoryChoicePanel(style);
		return dialog.ShowAsync(speaker, prompt, options, allowCancel, cancellationToken);
	}

	private StoryChoicePanel GetStoryChoicePanel(ChoiceStyle style) => style switch
	{
		ChoiceStyle.Regular => regularChoices,
		ChoiceStyle.Bold => boldChoices,
		_ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unsupported story choice style."),
	};

	public void Reset()
	{
		dialogue.ResetImmediate();
		regularChoices.HidePanel();
		boldChoices.HidePanel();
		video.ResetImmediate();
		intertitle.ResetImmediate();
		effects.ResetImmediate();
	}
}
