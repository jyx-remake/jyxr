using Game.Application;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSkillFragmentRewardBox : Panel
{
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private BattleSettlementSkillFragmentRewardView? _reward;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		Refresh();
	}

	public void Setup(BattleSettlementSkillFragmentRewardView reward)
	{
		ArgumentNullException.ThrowIfNull(reward);
		_reward = reward;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _reward is null)
		{
			return;
		}

		var description = BuildDescription(_reward);
		TooltipText = description;
		_avatar.Texture = ResolveIcon(_reward);
		_nameLabel.Text = _reward.DisplayName;
		_nameLabel.TooltipText = description;
	}

	private static string BuildDescription(BattleSettlementSkillFragmentRewardView reward)
	{
		var skillKind = reward.Kind switch
		{
			SkillFragmentKind.External => "外功",
			SkillFragmentKind.Internal => "内功",
			_ => "武学",
		};
		var levelText = reward.Levels == 1 ? "1 级" : $"{reward.Levels} 级";
		return $"{reward.DisplayName}：自动提高该{skillKind}等级上限 {levelText}。";
	}

	private static Texture2D? ResolveIcon(BattleSettlementSkillFragmentRewardView reward) =>
		reward.Kind switch
		{
			SkillFragmentKind.External when Game.ContentRepository.TryGetExternalSkill(reward.SkillId, out var skill) =>
				AssetResolver.LoadSkillIconResource(skill.Icon),
			SkillFragmentKind.Internal when Game.ContentRepository.TryGetInternalSkill(reward.SkillId, out var skill) =>
				AssetResolver.LoadSkillIconResource(skill.Icon),
			_ => null,
		};
}
