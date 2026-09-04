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

	/// <summary>
	/// Distance from the viewport top to the bottom edge of the top bar frame.
	/// Map views shrink their viewport to start below this line.
	/// </summary>
	public float TopSafeInset { get; private set; }

	/// <summary>
	/// Distance from the viewport bottom to the top edge of the bottom bar
	/// frame. Map views shrink their viewport to end above this line.
	/// </summary>
	public float BottomSafeInset { get; private set; }

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

		// Node rects are only final after the layout pass; re-measure whenever
		// the HUD (or the window) changes size so map views can follow.
		Resized += () => CallDeferred(MethodName.RefreshSafeInsets);
		CallDeferred(MethodName.RefreshSafeInsets);
	}

	private void RefreshSafeInsets()
	{
		if (!IsInsideTree())
		{
			return;
		}

		var frame = GetNodeOrNull<Control>("TopBar/Frame");
		var frame2 = GetNodeOrNull<Control>("BottomRight/Frame2");
		if (frame is null || frame2 is null)
		{
			return;
		}

		var viewportHeight = GetViewportRect().Size.Y;
		TopSafeInset = frame.GetGlobalRect().End.Y;
		BottomSafeInset = float.Max(0f, viewportHeight - frame2.GetGlobalRect().Position.Y);
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
		_goldLabel.Text = Game.Profile.Yuanbao.ToString();
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
			: AssetResolver.LoadTexture(hero.Portrait);
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
		$"当前难度：{GameDifficultyFormatter.FormatNameCn(adventure.Difficulty)}\n当前周目：{adventure.Round}";
}
