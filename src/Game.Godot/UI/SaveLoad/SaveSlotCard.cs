using Game.Application;
using Game.Application.Mods;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Godot.Resources;
using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI;

public partial class SaveSlotCard : Button
{
	private Label _titleLabel = null!;
	private Label _nameLabel = null!;
	private Label _partyCountLabel = null!;
	private Label _playTimeLabel = null!;
	private Label _gameTimeLabel = null!;
	private Label _difficultyLabel = null!;
	private Label _roundLabel = null!;
	private Label _locationLabel = null!;
	private Label _savedAtLabel = null!;
	private TextureRect _portrait = null!;
	private Control _latestBadge = null!;
	private Control _noRegretBadge = null!;
	private Control _structuralRiskBadge = null!;
	private Control _gameplayRiskBadge = null!;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_nameLabel = GetNode<Label>("%NameLabel");
		_partyCountLabel = GetNode<Label>("%PartyCountLabel");
		_playTimeLabel = GetNode<Label>("%PlayTimeLabel");
		_gameTimeLabel = GetNode<Label>("%GameTimeLabel");
		_difficultyLabel = GetNode<Label>("%DifficultyLabel");
		_roundLabel = GetNode<Label>("%RoundLabel");
		_locationLabel = GetNode<Label>("%LocationLabel");
		_savedAtLabel = GetNode<Label>("%SavedAtLabel");
		_portrait = GetNode<TextureRect>("%Portrait");
		_latestBadge = GetNode<Control>("%LatestBadge");
		_noRegretBadge = GetNode<Control>("%NoRegretBadge");
		_structuralRiskBadge = GetNode<Control>("%StructuralRiskBadge");
		_gameplayRiskBadge = GetNode<Control>("%GameplayRiskBadge");
	}

	public void Configure(
		LocalSaveSummary summary,
		SaveSlotPanelMode mode,
		bool isMostRecentNonAuto)
	{
		Disabled = mode switch
		{
			SaveSlotPanelMode.Save => false,
			SaveSlotPanelMode.Load => !summary.CanLoad,
			SaveSlotPanelMode.Delete => !summary.HasSave,
			_ => throw new InvalidOperationException($"Unsupported save slot panel mode: {mode}"),
		};
		Modulate = Disabled
			? new Color(1f, 1f, 1f, 0.55f)
			: Colors.White;
		_titleLabel.Text = summary.SaveId.Title;
		_latestBadge.Visible = summary.CanLoad && isMostRecentNonAuto;
		_noRegretBadge.Visible = summary.CanLoad && summary.NoRegret;
		_structuralRiskBadge.Visible = summary.CanLoad && summary.ModWarningImpact == SaveImpact.Structural;
		_gameplayRiskBadge.Visible = summary.CanLoad && summary.ModWarningImpact == SaveImpact.Gameplay;

		if (!summary.HasSave)
		{
			_portrait.Texture = null;
			_nameLabel.Text = "空档";
			_partyCountLabel.Text = string.Empty;
			_playTimeLabel.Text = string.Empty;
			_gameTimeLabel.Text = string.Empty;
			_difficultyLabel.Text = string.Empty;
			_roundLabel.Text = string.Empty;
			_locationLabel.Text = string.Empty;
			_savedAtLabel.Text = string.Empty;
			return;
		}

		if (!summary.CanLoad)
		{
			_portrait.Texture = null;
			_nameLabel.Text = BuildInvalidSlotTitle(summary.FailureReason);
			_partyCountLabel.Text = string.Empty;
			_playTimeLabel.Text = string.Empty;
			_gameTimeLabel.Text = string.Empty;
			_difficultyLabel.Text = string.Empty;
			_roundLabel.Text = string.Empty;
			_locationLabel.Text = string.Empty;
			_savedAtLabel.Text = string.Empty;
			return;
		}

		_portrait.Texture = AssetResolver.LoadTexture(summary.LeaderPortrait);
		_nameLabel.Text = summary.LeaderName ?? "无名侠客";
		_partyCountLabel.Text = $"队伍 {summary.PartyMemberCount}";
		_playTimeLabel.Text = $"游玩 {PlayTimeFormatter.FormatHoursAndMinutes(summary.PlayTimeSeconds)}";
		_gameTimeLabel.Text = BuildGameTimeText(summary.Clock);
		_difficultyLabel.Text = GameDifficultyFormatter.FormatNameCn(summary.Difficulty);
		_roundLabel.Text = $"周目 {summary.Round}";
		_locationLabel.Text = $"当前位置  {ResolveMapName(summary.CurrentMapId)}";
		_savedAtLabel.Text = summary.SavedAtUtc is null
			? string.Empty
			: $"保存于 {summary.SavedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}";
	}

	private static string BuildGameTimeText(ClockRecord? clockRecord)
	{
		if (clockRecord is null)
		{
			return string.Empty;
		}

		return ClockFormatter.FormatDateTimeCn(ClockState.Restore(clockRecord));
	}

	private static string ResolveMapName(string? mapId)
	{
		if (string.IsNullOrWhiteSpace(mapId))
		{
			return "未进入地图";
		}

		if (Game.ContentRepository.TryGetMap(mapId, out var map))
		{
			return map.Name;
		}

		Game.Logger.Warning($"Save slot map definition is missing: {mapId}");
		return mapId;
	}

	private static string BuildInvalidSlotTitle(LocalSaveReadFailureReason failureReason) => failureReason switch
	{
		LocalSaveReadFailureReason.EnvelopeVersionMismatch or LocalSaveReadFailureReason.SaveVersionMismatch
			=> "版本不兼容",
		LocalSaveReadFailureReason.InvalidFormat => "存档已损坏",
		LocalSaveReadFailureReason.MissingFile => "空档",
		_ => "无法读取",
	};
}
