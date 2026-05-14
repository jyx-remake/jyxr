using Game.Godot.UI;
using Godot;

namespace Game.Godot;

public static class GameFlow
{
	public const string MainMenuScenePath = "res://scenes/main_menu/main_menu.tscn";

	public static async Task StartNewGameAsync(CancellationToken cancellationToken = default)
	{
		Game.SessionFlowService.StartNewGame();
		await StartOpeningStoryAsync(cancellationToken);
	}

	public static async Task StartNextRoundAsync(CancellationToken cancellationToken = default)
	{
		Game.SessionFlowService.StartNextRound();
		await StartOpeningStoryAsync(cancellationToken);
	}

	public static void ReturnToMainMenu()
	{
		UIRoot.Instance.ClosePanel();
		UIRoot.Instance.SetHudSuppressed(true);
		UIRoot.Instance.SetStoryPresentationActive(false);
		Game.Audio.PlayBgm(Game.Config.MainMenuMusic);

		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			throw new InvalidOperationException("Godot scene tree is not available.");
		}

		var error = tree.ChangeSceneToFile(MainMenuScenePath);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Changing to main menu failed: {error}.");
		}
	}

	private static async Task StartOpeningStoryAsync(CancellationToken cancellationToken)
	{
		UIRoot.Instance.ClosePanel();
		UIRoot.Instance.SetHudSuppressed(false);
		UIRoot.Instance.SetStoryPresentationActive(true);
		
		var storyId = Game.Config.InitialStorySegmentId;
		try
		{
			await Game.StoryService.ExecuteAsync(storyId, cancellationToken);
		}
		finally
		{
			UIRoot.Instance.SetStoryPresentationActive(false);
		}
	}
}
