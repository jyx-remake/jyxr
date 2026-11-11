using Game.Application;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("select_sect", "select_menpai")]
	private async ValueTask ExecuteSelectSectAsync(CancellationToken cancellationToken)
	{
		var sect = await UIRoot.Instance.ShowSelectSectScreenAsync(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrWhiteSpace(sect.StoryId))
		{
			throw new InvalidOperationException($"Sect '{sect.Id}' does not define an entry story.");
		}

		await Game.StoryService.ExecuteAsync(sect.StoryId, cancellationToken: cancellationToken);
	}

	[StoryCommand("input_name")]
	private async ValueTask ExecuteInputNameAsync(
		string characterId,
		string defaultName = "",
		CancellationToken cancellationToken = default)
	{
		var name = await UIRoot.Instance.ShowInputNamePanelAsync(characterId, defaultName, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		Game.PartyService.RenameOrCreateReserve(characterId, name);
	}

	[StoryCommand("select_portrait", "select_head")]
	private async ValueTask ExecuteSelectHeadAsync(string characterId, CancellationToken cancellationToken)
	{
		var head = await UIRoot.Instance.ShowSelectHeadPanelAsync(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		Game.CharacterService.SetCharacterPortrait(characterId, head);
	}

	[StoryCommand("roll_stats")]
	private ValueTask ExecuteRollStatsAsync(CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowRollStatsPanelAsync("主角", cancellationToken));
}
