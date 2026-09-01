using Game.Application;
using Game.Core.Persistence;
using Game.Godot.Story;
using Game.Godot.UI;
using Godot;

namespace Game.Godot;

public static class GameFlow
{
	public const string MainMenuScenePath = "res://scenes/main_menu/main_menu.tscn";
	public static bool IsMainMenuActive =>
		Engine.GetMainLoop() is SceneTree { CurrentScene: MainMenu { Visible: true } };

	public static async Task StartNewGameAsync(CancellationToken cancellationToken = default)
	{
		Game.SessionFlowService.StartNewGame();
		World.Instance.PlayTime.StartGameplay();
		await StartOpeningStoryAsync(cancellationToken);
	}

	public static async Task StartNextRoundAsync(CancellationToken cancellationToken = default)
	{
		Game.SessionFlowService.StartNextRound();
		World.Instance.PlayTime.StartGameplay();
		await StartOpeningStoryAsync(cancellationToken);
	}

	public static void LoadSave(SaveGame saveGame)
	{
		ArgumentNullException.ThrowIfNull(saveGame);

		Game.SaveGameService.LoadSave(saveGame);
		World.Instance.PlayTime.StartGameplay();
		UIRoot.Instance.ResetPresentationAfterLoad();

		if (string.IsNullOrWhiteSpace(Game.State.Location.CurrentMapId))
		{
			return;
		}

		if (Engine.GetMainLoop() is SceneTree { CurrentScene: MainMenu mainMenu })
		{
			mainMenu.Hide();
		}

		UIRoot.Instance.SetHudSuppressed(false);
		UIRoot.Instance.ShowHud();
	}

	public static void ReturnToMainMenu()
	{
		World.Instance.PlayTime.StopGameplay();
		World.Instance.ClearCurrentScene();
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

	public static void GameOver()
	{
		World.Instance.PlayTime.StopGameplay();
		Game.ProfileService.AddDeaths();
		UIRoot.Instance.ShowGameOverScreen();
	}

	public static void GameComplete()
	{
		World.Instance.PlayTime.StopGameplay();
		UIRoot.Instance.ShowGameFinScreen();
	}

	private static async Task StartOpeningStoryAsync(CancellationToken cancellationToken)
	{
		UIRoot.Instance.ClosePanel();
		UIRoot.Instance.SetHudSuppressed(false);
		UIRoot.Instance.SetStoryPresentationActive(true);
		
		var storyId = Game.Config.InitialStorySegmentId;
		try
		{
			var result = await StoryRunHelper.RunAsync(storyId, cancellationToken);
			if (result.SegmentCompleted)
			{
				Game.Session.Events.Publish(new AutoSaveRequestedEvent($"story '{storyId}' completed"));
			}
		}
		finally
		{
			UIRoot.Instance.SetStoryPresentationActive(false);
		}
	}
}
