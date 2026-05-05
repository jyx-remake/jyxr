using System.Globalization;
using System.Text;
using Game.Application;
using Game.Core.Definitions;
using Godot;

namespace Game.Godot.UI;

public partial class HeroPanel : JyPanel
{
	private const string AchievementGroup = "nick";

	private TabContainer _heroTabContainer = null!;
	private JyButton _adventureTabButton = null!;
	private JyButton _achievementTabButton = null!;
	private JyButton _masteryTabButton = null!;
	private RichTextLabel _adventurePageSubtitle = null!;
	private CheckBox _previewCheckBox = null!;
	private Label _completionLabel = null!;
	private RichTextLabel _achievementLabel = null!;
	private readonly List<IDisposable> _subscriptions = [];

	public override void _Ready()
	{
		base._Ready();

		_heroTabContainer = GetNode<TabContainer>("%HeroTabContainer");
		_adventureTabButton = GetNode<JyButton>("%AdventureTabButton");
		_achievementTabButton = GetNode<JyButton>("%AchievementTabButton");
		_masteryTabButton = GetNode<JyButton>("%MasteryTabButton");
		_adventurePageSubtitle = GetNode<RichTextLabel>("%AdventurePageSubtitle");
		_previewCheckBox = GetNode<CheckBox>("%PreviewCheckBox");
		_completionLabel = GetNode<Label>("%CompletionLabel");
		_achievementLabel = GetNode<RichTextLabel>("%AchievementLabel");

		_adventureTabButton.Pressed += () => ShowTab(0);
		_achievementTabButton.Pressed += () => ShowTab(1);
		_masteryTabButton.Pressed += () => ShowTab(2);
		_previewCheckBox.Toggled += OnPreviewToggled;
		_subscriptions.Add(Game.Session.Events.Subscribe<ProfileChangedEvent>(OnProfileChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<ProfileLoadedEvent>(OnProfileLoaded));
		_subscriptions.Add(Game.Session.Events.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked));
		_subscriptions.Add(Game.Session.Events.Subscribe<AdventureStateChangedEvent>(OnAdventureStateChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));

		RenderAdventure();
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
		_heroTabContainer.CurrentTab = index;
	}

	private void OnProfileChanged(ProfileChangedEvent _) => RenderAchievements();

	private void OnProfileLoaded(ProfileLoadedEvent _) => RenderAchievements();

	private void OnAchievementUnlocked(AchievementUnlockedEvent _) => RenderAchievements();

	private void OnAdventureStateChanged(AdventureStateChangedEvent _) => RenderAdventure();

	private void OnSaveLoaded(SaveLoadedEvent _)
	{
		RenderAdventure();
		RenderAchievements();
	}

	private void RenderAdventure()
	{
		var sectName = ResolveSectName();
		var morality = Game.State.Adventure.Morality;
		var favorability = Game.State.Adventure.Favorability;
		_adventurePageSubtitle.Text =
			$"门派：[color=red]{sectName}[/color]\n道德：[color=red]{morality}[/color]\n女主角好感：[color=red]{favorability}[/color]";
	}

	private void RenderAchievements()
	{
		var achievements = Game.ContentRepository.GetResourcesByGroup(AchievementGroup);
		_completionLabel.Text = BuildCompletionText(achievements);
		_achievementLabel.Text = BuildAchievementsText(achievements, _previewCheckBox.ButtonPressed);
	}

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
}
