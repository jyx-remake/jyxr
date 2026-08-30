using System.Globalization;
using System.Text;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Presentation.Hero;
using Godot;

namespace Game.Godot.UI;

public partial class HeroPanel : JyPanel
{
	private const string AchievementGroup = "nick";

	[Export]
	public PackedScene SkillBoxScene { get; set; } = null!;

	private TabContainer _heroTabContainer = null!;
	private JyButton _adventureTabButton = null!;
	private JyButton _achievementTabButton = null!;
	private JyButton _masteryTabButton = null!;
	private RichTextLabel _adventurePageSubtitle = null!;
	private RichTextLabel _profileStatsLabel = null!;
	private global::Godot.Timer _profileStatsRefreshTimer = null!;
	private CheckBox _previewCheckBox = null!;
	private Label _completionLabel = null!;
	private RichTextLabel _achievementLabel = null!;
	private HeroSkillMasteryView _masteryView = null!;
	private bool _isMasteryViewInitialized;
	private readonly List<IDisposable> _subscriptions = [];

	public override void _Ready()
	{
		base._Ready();

		_heroTabContainer = GetNode<TabContainer>("%HeroTabContainer");
		_adventureTabButton = GetNode<JyButton>("%AdventureTabButton");
		_achievementTabButton = GetNode<JyButton>("%AchievementTabButton");
		_masteryTabButton = GetNode<JyButton>("%MasteryTabButton");
		_adventurePageSubtitle = GetNode<RichTextLabel>("%AdventurePageSubtitle");
		_profileStatsLabel = GetNode<RichTextLabel>("%ProfileStatsLabel");
		_profileStatsRefreshTimer = GetNode<global::Godot.Timer>("%ProfileStatsRefreshTimer");
		_previewCheckBox = GetNode<CheckBox>("%PreviewCheckBox");
		_completionLabel = GetNode<Label>("%CompletionLabel");
		_achievementLabel = GetNode<RichTextLabel>("%AchievementLabel");
		_masteryView = CreateMasteryView();

		_adventureTabButton.Pressed += () => ShowTab(0);
		_achievementTabButton.Pressed += () => ShowTab(1);
		_masteryTabButton.Pressed += () => ShowTab(2);
		_previewCheckBox.Toggled += OnPreviewToggled;
		_profileStatsRefreshTimer.Timeout += OnProfileStatsRefreshTimeout;
		_subscriptions.Add(Game.Session.Events.Subscribe<ProfileChangedEvent>(OnProfileChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<ProfileLoadedEvent>(OnProfileLoaded));
		_subscriptions.Add(Game.Session.Events.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked));
		_subscriptions.Add(Game.Session.Events.Subscribe<AdventureStateChangedEvent>(OnAdventureStateChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));

		Game.PlayTimeService.Checkpoint();
		RenderAdventure();
		RenderProfileStats();
		RenderAchievements();
		ShowTab(0);
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private void ShowTab(int index)
	{
		if (index == 2 && !_isMasteryViewInitialized)
		{
			_masteryView.Initialize();
			_isMasteryViewInitialized = true;
		}

		_heroTabContainer.CurrentTab = index;
	}

	private void OnProfileChanged(ProfileChangedEvent _)
	{
		RenderProfileStats();
		RenderAchievements();
	}

	private void OnProfileLoaded(ProfileLoadedEvent _)
	{
		RenderProfileStats();
		RenderAchievements();
	}

	private void OnAchievementUnlocked(AchievementUnlockedEvent _) => RenderAchievements();

	private void OnAdventureStateChanged(AdventureStateChangedEvent _) => RenderAdventure();

	private void OnSaveLoaded(SaveLoadedEvent _)
	{
		RenderAdventure();
		RenderProfileStats();
		RenderAchievements();
	}

	private void OnProfileStatsRefreshTimeout()
	{
		Game.PlayTimeService.Checkpoint();
		RenderProfileStats();
	}

	private void RenderAdventure()
	{
		var adventure = Game.State.Adventure;
		var heroName = Game.State.Party.TryGetMember(Party.HeroCharacterId, out var hero)
			? hero.Name
			: Party.HeroCharacterId;
		var noRegretTag = adventure.NoRegret
			? " [color=#c58aa7]【无悔】[/color]"
			: string.Empty;
		var builder = new StringBuilder(
			$"[center][font_size=34][color=#f0ebe3]本周目历练[/color][/font_size]\n[font_size=30][color=#b0f9f9]{heroName}基本信息[/color][/font_size]\n[table=4]");
		AppendAdventureStat(builder, "门派", ResolveSectName(), "#fbf8f1");
		AppendAdventureStat(builder, "道德", adventure.Morality.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendAdventureStat(
			builder,
			"难度",
			GameDifficultyFormatter.FormatNameCn(adventure.Difficulty) + noRegretTag,
			"#d86b62");
		AppendAdventureStat(builder, "周目", adventure.Round.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendAdventureStat(
			builder,
			"血内上限",
			Game.CharacterResourceLimitPolicy.GetMaxHpMp().ToString(CultureInfo.InvariantCulture),
			"#fbf8f1");
		AppendAdventureStat(
			builder,
			"箱子",
			$"{Game.State.Chest.GetStoredItemCount()}/{Game.ChestService.GetCapacity()}",
			"#fbf8f1");
		AppendAdventureStat(builder, "声望", FormatNumber(adventure.Rank), "#fbf8f1");
		AppendAdventureStat(builder, "性格", ResolvePersonality(hero), "#fbf8f1");
		AppendAdventureStat(builder, "等级上限", Game.Config.MaxLevel.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendAdventureStat(builder, "属性上限", Game.Config.MaxAttribute.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendAdventureStat(builder, "武学掉落", ResolveSkillDropTier(adventure.Round), "#fbf8f1");
		AppendAdventureStat(builder, "结义", ResolveBrotherhood(adventure), "#fbf8f1");
		builder.Append("[/table]\n[table=2]");
		AppendAdventureStat(builder, "内功等级上限", ResolveSkillLevelCap(Game.Config.BaseInternalSkillMaxLevel, adventure.Round).ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendAdventureStat(builder, "外功等级上限", ResolveSkillLevelCap(Game.Config.BaseExternalSkillMaxLevel, adventure.Round).ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		builder.Append("[/table]\n[font_size=32][color=#f0ebe3]人物好感[/color][/font_size]\n");
		AppendFavorabilityGrid(builder);
		builder.Append("[/center]");
		_adventurePageSubtitle.Text = builder.ToString();
	}

	private void RenderProfileStats()
	{
		var profile = Game.Profile;
		var builder = new StringBuilder(
			"[center][font_size=34][color=#f0ebe3]生涯留痕[/color][/font_size]\n[table=3]");
		AppendProfileStat(builder, "死亡次数", profile.DeathCount.ToString(CultureInfo.InvariantCulture), "#d86b62");
		AppendProfileStat(builder, "存档次数", profile.SaveCount.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendProfileStat(builder, "击杀数量", profile.KillCount.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendProfileStat(builder, "通关次数", profile.CompletionCount.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendProfileStat(builder, "最高周目", profile.HighestRound.ToString(CultureInfo.InvariantCulture), "#fbf8f1");
		AppendProfileStat(builder, "总游玩时间", PlayTimeFormatter.FormatHoursAndMinutes(profile.TotalPlayTimeSeconds), "#fbf8f1");
		builder.Append("[/table][/center]");
		_profileStatsLabel.Text = builder.ToString();
	}

	private static void AppendAdventureStat(StringBuilder builder, string title, string value, string valueColor)
	{
		builder.Append("[cell expand=1 shrink=false padding=18,12,18,12][center][font_size=32][color=#e8e2d9]");
		builder.Append(title);
		builder.Append("[/color][/font_size]\n[font_size=38][color=");
		builder.Append(valueColor);
		builder.Append(']');
		builder.Append(value);
		builder.Append("[/color][/font_size][/center][/cell]");
	}

	private static void AppendProfileStat(StringBuilder builder, string title, string value, string valueColor)
	{
		builder.Append("[cell expand=1 shrink=false padding=30,14,30,14][center][font_size=32][color=#e8e2d9]");
		builder.Append(title);
		builder.Append("[/color][/font_size]\n[font_size=40][color=");
		builder.Append(valueColor);
		builder.Append(']');
		builder.Append(value);
		builder.Append("[/color][/font_size][/center][/cell]");
	}

	private static void AppendFavorabilityGrid(StringBuilder builder)
	{
		var favorabilityViews = HeroFavorabilityPresenter.Build(
			Game.State.Adventure,
			Game.State.Party,
			Game.ContentRepository);
		if (favorabilityViews.Count == 0)
		{
			builder.Append("[font_size=32][color=#bbb5ab]暂无[/color][/font_size]");
			return;
		}

		builder.Append("[table=4]");
		foreach (var favorability in favorabilityViews)
		{
			builder.Append("[cell expand=1 shrink=false padding=18,8,18,8][center][font_size=32][color=#f0ebe3]");
			builder.Append(favorability.DisplayName);
			builder.Append("[/color]  [color=#fbf8f1]");
			builder.Append(favorability.Value);
			builder.Append("[/color][/font_size][/center][/cell]");
		}
		builder.Append("[/table]");
	}

	private void RenderAchievements()
	{
		var achievements = Game.ContentRepository.GetResourcesByGroup(AchievementGroup);
		_completionLabel.Text = BuildCompletionText(achievements);
		_achievementLabel.Text = BuildAchievementsText(achievements, _previewCheckBox.ButtonPressed);
	}

	private HeroSkillMasteryView CreateMasteryView() =>
		new(
			GetNode<GridContainer>("%GridContainer"),
			SkillBoxScene,
			new HeroSkillMasteryPresenter(
				Game.ContentRepository,
				Game.SkillMaxLevelPolicy,
				Game.Config.AbsoluteSkillMaxLevel),
			GetNode<JyButton>("%AllButton"),
			GetNode<JyButton>("%QuanzhangButton"),
			GetNode<JyButton>("%JianfaButton"),
			GetNode<JyButton>("%DaofaButton"),
			GetNode<JyButton>("%QimenButton"),
			GetNode<JyButton>("%NeigongButton"),
			GetNode<CheckBox>("%MasteryPreviewHardMaxCheckBox"));

	private void OnPreviewToggled(bool _) =>
		RenderAchievements();

	private static string BuildAchievementsText(
		IReadOnlyList<ResourceDefinition> achievements,
		bool isPreviewEnabled)
	{
		var builder = new StringBuilder();
		for (var index = 0; index < achievements.Count; index++)
		{
			var achievement = achievements[index];
			var title = GetAchievementTitle(achievement);
			var description = achievement.Value.Trim();
			var isUnlocked = Game.Profile.IsAchievementUnlocked(title);
			var color = isUnlocked ? "green" : "red";

			builder.Append("[color=");
			builder.Append(color);
			builder.Append(']');
			builder.Append(title);
			if (isUnlocked || isPreviewEnabled)
			{
				if (!string.IsNullOrWhiteSpace(description))
				{
					builder.Append(": ");
					builder.Append(description);
				}
			}
			else
			{
				builder.Append(": 尚未解锁");
			}
			builder.Append("[/color]");

			if (index < achievements.Count - 1)
			{
				builder.AppendLine();
			}
		}

		return builder.ToString();
	}

	private static string BuildCompletionText(IReadOnlyList<ResourceDefinition> achievements)
	{
		if (achievements.Count == 0)
		{
			return "完成度：0.00%";
		}

		var unlockedCount = achievements.Count(achievement => Game.Profile.IsAchievementUnlocked(GetAchievementTitle(achievement)));
		var completionRate = unlockedCount * 100d / achievements.Count;
		return $"完成度：{completionRate.ToString("0.00", CultureInfo.InvariantCulture)}%";
	}

	private static string ResolveSectName()
	{
		var sectId = Game.State.Adventure.SectId;
		if (string.IsNullOrWhiteSpace(sectId))
		{
			return "无门派";
		}

		if (Game.ContentRepository.TryGetSect(sectId, out var sect))
		{
			return sect.Name;
		}

		Game.Logger.Warning($"Hero panel sect definition is missing: {sectId}");
		return sectId;
	}

	private static string GetAchievementTitle(ResourceDefinition achievement)
	{
		const string prefix = "nick.";
		return achievement.Id.StartsWith(prefix, StringComparison.Ordinal)
			? achievement.Id[prefix.Length..]
			: achievement.Id;
	}

	private static string FormatNumber(double value) =>
		value.ToString("0.##", CultureInfo.InvariantCulture);

	private static string ResolvePersonality(CharacterInstance? hero) => hero?.Personality switch
	{
		1 => "功利",
		2 => "古怪",
		3 => "正直",
		4 => "豁达",
		_ => "无",
	};

	private static string ResolveSkillDropTier(int round) => round switch
	{
		>= 4 => "天阶",
		3 => "地阶",
		2 => "玄阶",
		_ => "黄阶",
	};

	private static int ResolveSkillLevelCap(int baseLevel, int round)
	{
		var roundBonus = Math.Max(0, (round - 1) / Math.Max(1, Game.Config.RoundsPerMaxSkillLevelIncrease));
		return Math.Min(Game.Config.AbsoluteSkillMaxLevel, baseLevel + roundBonus);
	}

	private static string ResolveBrotherhood(AdventureState adventure)
	{
		var names = new[]
		{
			"无", "黄蓉", "岳灵珊", "周芷若", "李文秀", "小龙女", "小昭", "侍剑",
			"郭襄", "穆念慈", "王语嫣", "萧中慧", "袁紫衣", "霍青桐", "香香公主",
		};
		var value = adventure.GetFavorability("结义");
		return value is >= 51 and <= 64 ? names[value - 50] : names[0];
	}
}
