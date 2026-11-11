using Game.Application;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("set_portrait", "head")]
	private ValueTask ExecuteHeadAsync(string characterId, string portraitId)
	{
		Game.CharacterService.SetCharacterPortrait(characterId, portraitId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("set_model", "animation")]
	private ValueTask ExecuteAnimationAsync(string characterId, string modelId)
	{
		Game.CharacterService.SetCharacterModel(characterId, modelId);
		return ValueTask.CompletedTask;
	}

	public ValueTask MainMenuAsync(CancellationToken cancellationToken)
	{
		GameFlow.ReturnToMainMenu();
		return ValueTask.CompletedTask;
	}

	[StoryCommand("restart")]
	private async ValueTask ExecuteRestartAsync(CancellationToken cancellationToken)
	{
		Game.ProfileService.RecordCompletion(Game.State.Adventure.Round);
		// Replacing State cancels this execution. The opening story belongs to the new State.
		await GameFlow.RestartCurrentRoundAsync();
	}

	[StoryCommand("next_round", "nextzhoumu")]
	private ValueTask ExecuteNextZhoumuAsync(CancellationToken cancellationToken)
	{
		Game.ProfileService.RecordCompletion(Game.State.Adventure.Round);
		return new ValueTask(GameFlow.StartNextRoundAsync());
	}

	public ValueTask GameOverAsync(CancellationToken cancellationToken)
	{
		GameFlow.GameOver();
		return ValueTask.CompletedTask;
	}

	public ValueTask GameCompleteAsync(CancellationToken cancellationToken)
	{
		GameFlow.GameComplete();
		return ValueTask.CompletedTask;
	}
}
