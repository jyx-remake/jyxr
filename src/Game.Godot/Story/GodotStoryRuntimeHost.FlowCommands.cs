using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("set_gender")]
	private ValueTask ExecuteGenderAsync(string characterId, string gender)
	{
		var parsed = gender.Trim().ToLowerInvariant() switch
		{
			"male" or "男性" => CharacterGender.Male,
			"female" or "女性" => CharacterGender.Female,
			"neutral" or "中性" => CharacterGender.Neutral,
			"animal" or "动物" => CharacterGender.Animal,
			"eunuch" or "太监" => CharacterGender.Eunuch,
			_ => throw new InvalidOperationException($"Unknown character gender '{gender}'."),
		};

		Game.CharacterService.SetCharacterGender(characterId, parsed);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("set_personality")]
	private ValueTask ExecutePersonalityAsync(string characterId, int personality)
	{
		Game.CharacterService.SetPersonality(characterId, personality);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("set_personality_random")]
	private ValueTask ExecuteRandomPersonalityAsync(string characterId)
	{
		Game.CharacterService.SetPersonality(
			characterId,
			Game.Session.RandomService.Next(1, 5));
		return ValueTask.CompletedTask;
	}

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

	[StoryCommand("main_menu", "mainmenu")]
	private ValueTask ExecuteMainMenuAsync()
	{
		GameFlow.ReturnToMainMenu();
		return ValueTask.CompletedTask;
	}

	[StoryCommand("restart")]
	private async ValueTask ExecuteRestartAsync(CancellationToken cancellationToken)
	{
		Game.ProfileService.RecordCompletion(Game.State.Adventure.Round);
		await GameFlow.RestartCurrentRoundAsync(cancellationToken);
	}

	[StoryCommand("next_round", "nextzhoumu")]
	private ValueTask ExecuteNextZhoumuAsync(CancellationToken cancellationToken)
	{
		Game.ProfileService.RecordCompletion(Game.State.Adventure.Round);
		return new ValueTask(GameFlow.StartNextRoundAsync(cancellationToken));
	}

	[StoryCommand("game_over", "gameover")]
	private ValueTask<StoryCommandResult> ExecuteGameOverAsync()
	{
		GameFlow.GameOver();
		return ValueTask.FromResult(StoryCommandResult.Terminate);
	}

	[StoryCommand("game_complete", "gamefin")]
	private ValueTask ExecuteGameFinAsync()
	{
		Game.ProfileService.RecordCompletion(Game.State.Adventure.Round);
		GameFlow.GameComplete();
		return ValueTask.CompletedTask;
	}
}
