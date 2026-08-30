using Game.Application;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> XmjhPortraitSets =
		new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["xmjh_male"] =
			[
				"头像.侠客小虾米", "头像.头像1", "头像.头像2", "头像.虾米主角2", "头像.头像3",
				"头像.头像4", "头像.虾米主角4", "头像.头像6", "头像.头像7", "头像.剑客道士",
				"头像.魔君", "头像.头像9", "头像.造作君", "头像.头像8", "头像.绿衣公子",
				"头像.公子子", "头像.头像10", "头像.头像11", "头像.虾米主角3", "头像.虾米主角1",
				"头像.头像13", "头像.头像14", "头像.斗笠男", "头像.头像12", "头像.主角4",
			],
			["xmjh_female"] =
			[
				"头像.红衣女", "头像.剑客女", "头像.明月心", "头像.花旦", "头像.仙女", "头像.琴女",
				"头像.头像女3", "头像.女帝", "头像.美丽女", "头像.清纯女", "头像.美女3", "头像.蓝衣女剑",
				"头像.古典女", "头像.侠客女", "头像.披风女", "头像.头像女1", "头像.头像女2", "头像.鸟女",
				"头像.头像女4", "头像.玉玲珑", "头像.萝莉", "头像.斗笠女", "头像.萝莉2", "头像.红姬", "头像.军娘",
			],
			["xmjh_special"] =
			[
				"头像.郭靖", "头像.黄飞鸿", "头像.田伯光", "头像.独孤求败", "头像.赞助豪名", "头像.魂穿黄蓉", "头像.狄云",
			],
		};

	[StoryCommand("select_sect", "select_menpai")]
	private async ValueTask ExecuteSelectSectAsync(CancellationToken cancellationToken)
	{
		var sect = await UIRoot.Instance.ShowSelectSectScreenAsync(cancellationToken);
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
		Game.PartyService.RenameOrCreateReserve(characterId, name);
	}

	[StoryCommand("select_portrait", "select_head")]
	private async ValueTask ExecuteSelectHeadAsync(
		string characterId,
		string portraitSet = "default",
		CancellationToken cancellationToken = default)
	{
		var portraits = XmjhPortraitSets.TryGetValue(portraitSet.Trim(), out var selected)
			? selected
			: null;
		var head = await UIRoot.Instance.ShowSelectHeadPanelAsync(portraits, cancellationToken);
		Game.CharacterService.SetCharacterPortrait(characterId, head);
	}

	[StoryCommand("roll_stats")]
	private ValueTask ExecuteRollStatsAsync(
		string characterId = "主角",
		string rollMode = "default",
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowRollStatsPanelAsync(characterId, rollMode, cancellationToken));
}
