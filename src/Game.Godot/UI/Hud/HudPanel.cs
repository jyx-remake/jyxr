using System.Linq;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class HudPanel : Control
{
	private Label _mapLabel = null!;
	private Label _dateTimeLabel = null!;
	private Label _silverLabel = null!;
	private Label _goldLabel = null!;
	private TextureRect _runInfo = null!;
	private AvatarBox _heroBox = null!;
	private JyButton _heroButton = null!;
	private JyButton _teamButton = null!;
	private JyButton _backpackButton = null!;
	private JyButton _logButton = null!;
	private JyButton _systemButton = null!;

	public override void _Ready()
	{
		_mapLabel = GetNode<Label>("%MapLabel");
		_dateTimeLabel = GetNode<Label>("%DataTimeLabel");
		_silverLabel = GetNode<Label>("%SilverIngotLabel");
		_goldLabel = GetNode<Label>("%GlodIngotLabel");
		_runInfo = GetNode<TextureRect>("%RunInfo");
		_heroBox = GetNode<AvatarBox>("%HeroBox");
		_heroButton = GetNode<JyButton>("%HeroButton");
		_teamButton = GetNode<JyButton>("%TeamButton");
		_backpackButton = GetNode<JyButton>("%BackpackButton");
		_logButton = GetNode<JyButton>("%LogButton");
		_systemButton = GetNode<JyButton>("%SystemButton");

		_heroButton.Pressed += OnHeroButtonPressed;
		_teamButton.Pressed += OnTeamButtonPressed;
		_backpackButton.Pressed += () => UIRoot.Instance.ShowInventoryPanel();
		_logButton.Pressed += () => UIRoot.Instance.ShowGameLogPanel();
		_systemButton.Pressed += () => UIRoot.Instance.ShowSystemPanel();
	}

	public void Refresh()
	{
		if (!Game.IsInitialized)
		{
			return;
		}

		_mapLabel.Text = ResolveCurrentMapName();
		_dateTimeLabel.Text = ClockFormatter.FormatDateTimeCn(Game.State.Clock);
		_silverLabel.Text = Game.State.Currency.Silver.ToString();
		_goldLabel.Text = Game.State.Currency.Gold.ToString();
		_runInfo.TooltipText = BuildAdventureInfoTooltip(Game.State.Adventure);
		_heroBox.SetAvatarTexture(ResolveHeroPortrait());
	}

	private void OnHeroButtonPressed() => UIRoot.Instance.ShowHeroPanel();

	private void OnTeamButtonPressed() => UIRoot.Instance.ShowPartyPanel();

	private static string ResolveCurrentMapName()
	{
		var mapId = Game.State.Location.CurrentMapId;
		if (string.IsNullOrWhiteSpace(mapId))
		{
			return string.Empty;
		}

		if (Game.ContentRepository.TryGetMap(mapId, out var map))
		{
			return map.Name;
		}

		Game.Logger.Warning($"HUD map definition is missing: {mapId}");
		return mapId;
	}

	private static Texture2D? ResolveHeroPortrait()
	{
		var hero = TryGetHero();
		return hero is null
			? null
			: AssetResolver.LoadCharacterPortrait(hero);
	}

	private static CharacterInstance? TryGetHero()
	{
		var party = Game.State.Party;
		if (party.TryGetMember(PartyAccess.HeroCharacterId, out var hero))
		{
			return hero;
		}

		return party.Members.FirstOrDefault();
	}

	private static string BuildAdventureInfoTooltip(AdventureState adventure) =>
		$"当前难度：{FormatDifficulty(adventure.Difficulty)}\n当前周目：{adventure.Round}";

	private static string FormatDifficulty(GameDifficulty difficulty) => difficulty switch
	{
		GameDifficulty.Normal => "简单",
		GameDifficulty.Hard => "进阶",
		GameDifficulty.Crazy => "炼狱",
		_ => throw new InvalidOperationException($"Unsupported difficulty: {difficulty}"),
	};
}
