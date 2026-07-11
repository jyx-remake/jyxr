using Game.Core.Battle;
using Game.Core.Model.Skills;
using Game.Godot.Audio;
using Godot;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleEventPresenter(
    BattleBoardView board,
    RichTextLabel logLabel,
    Func<BattleState?> stateProvider,
    Func<BattleMessage, bool> schedule)
{
    private const string RestSfxId = "音效.休息";
    private static readonly Color DamageColor = Colors.White;
    private static readonly Color CriticalColor = Colors.Yellow;
    private static readonly Color HealColor = Colors.Green;
    private static readonly Color ManaColor = Colors.Blue;
    private static readonly Color StateColor = Colors.Red;
    private static readonly Color SpecialColor = Colors.Magenta;
    private static readonly Color InfoColor = Colors.Yellow;
    private readonly List<string> _logLines = [];

    public void Clear() { _logLines.Clear(); RefreshLog(); }

    public void Refresh() => RefreshLog();

    public void AppendResult(BattleCommandResult<BattleActionResult> result)
    {
        if (!string.IsNullOrWhiteSpace(result.Message)) AppendLog(result.Message);
        AppendMessages(result.Messages);
    }

    public void AppendMessages(IReadOnlyList<BattleMessage> messages)
    {
        foreach (var message in messages)
            if (!schedule(message)) PresentImmediate(message);
    }

    public void PresentImmediate(BattleMessage message)
    {
        var state = stateProvider();
        if (state is null) return;

        if (message is BattleCue cue)
        {
            if (cue.Kind == BattleCueKind.SpeechRequested && !string.IsNullOrWhiteSpace(cue.Speech?.Text))
                board.PlaySpeech(cue.UnitId, cue.Speech.Text);
            else if (cue.Kind == BattleCueKind.FloatTextRequested && !string.IsNullOrWhiteSpace(cue.FloatText?.Text))
                board.PlayFloatText(cue.UnitId, cue.FloatText.Text, ResolveFloatTextColor(cue.FloatText.Style));
            return;
        }

        if (message is not BattleFact fact) return;
        var unitName = state.TryGetUnit(fact.UnitId)?.Character.Name ?? fact.UnitId;
        switch (fact.Kind)
        {
            case BattleFactKind.SkillCast when fact.SkillCast is { } skill:
                board.PlayFloatText(fact.UnitId, skill.ResolvedSkillName, ResolveSkillColor(skill));
                AppendLog(skill.IsLegend
                    ? $"{unitName} 施展奥义【{skill.ResolvedSkillName}】。"
                    : $"{unitName} 施展【{skill.ResolvedSkillName}】。");
                break;
            case BattleFactKind.Damaged:
                PresentDamage(fact, unitName);
                break;
            case BattleFactKind.BuffApplied:
                var applied = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, applied, StateColor);
                AppendLog($"{unitName} 获得状态【{applied}】。");
                break;
            case BattleFactKind.BuffResisted:
                var resisted = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, "抵抗", InfoColor);
                AppendLog($"{unitName} 抵抗了状态【{resisted}】。");
                break;
            case BattleFactKind.BuffRemoved:
                board.PlayFloatText(fact.UnitId, $"{ResolveBuffName(fact.Detail)}解除", InfoColor);
                break;
            case BattleFactKind.Rested:
                PresentRest(fact, unitName);
                break;
            case BattleFactKind.ItemUsed:
                board.PlayFloatText(fact.UnitId, ResolveItemName(fact.Detail), InfoColor);
                break;
            case BattleFactKind.ActionSkipped:
                var reason = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, $"{reason}中", StateColor);
                AppendLog($"{unitName} 因【{reason}】无法行动。");
                break;
            case BattleFactKind.SkillLeveledUp when fact.SkillExperience is { } experience:
                board.PlayFloatText(
                    fact.UnitId,
                    experience.SkillKind == SkillKind.Internal
                        ? $"{experience.SkillName}等级提升"
                        : $"{experience.SkillName}等级升级",
                    HealColor);
                break;
            case BattleFactKind.CharacterLeveledUp when fact.CharacterExperience is not null:
                board.PlayFloatText(fact.UnitId, "角色等级提升", HealColor);
                break;
            case BattleFactKind.DefeatPrevented:
                AppendLog($"{unitName} 的天赋【{ResolveTalentName(fact.Detail)}】发动，避免了被击败。");
                break;
        }
    }

    public void AppendLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _logLines.Add(text);
        if (_logLines.Count > 12) _logLines.RemoveAt(0);
        if (logLabel.IsNodeReady()) RefreshLog();
    }

    private void PresentDamage(BattleFact fact, string unitName)
    {
        var damage = fact.Damage?.Amount ?? 0;
        var critical = damage > 0 && fact.Damage?.IsCritical == true;
        var extraStrike = string.Equals(fact.Detail, "extra_strike", StringComparison.Ordinal);
        if (damage > 0 && !string.Equals(fact.Damage?.SourceUnitId, fact.UnitId, StringComparison.Ordinal))
            board.PlayHit(fact.UnitId);
        board.PlayFloatText(
            fact.UnitId,
            damage <= 0 ? "MISS" : extraStrike ? $"多重攻击-{damage}" : critical ? $"暴击 -{damage}" : $"-{damage}",
            critical ? CriticalColor : DamageColor);
        AppendLog(extraStrike
            ? $"多重攻击！！{unitName} 受到 {damage} 点伤害。"
            : critical ? $"暴击！！{unitName} 受到 {damage} 点伤害。" : $"{unitName} 受到 {damage} 点伤害。");
    }

    private void PresentRest(BattleFact fact, string unitName)
    {
        var hp = fact.Rest?.Hp ?? 0;
        var mp = fact.Rest?.Mp ?? 0;
        AppendLog($"{unitName}休息。");
        if (hp > 0)
        {
            board.PlayFloatText(fact.UnitId, $"+{hp}", HealColor);
            AppendLog($"{unitName}回复生命值{hp}");
        }
        if (mp > 0)
        {
            board.PlayFloatText(fact.UnitId, $"+{mp}", ManaColor);
            AppendLog($"{unitName}回复内力{mp}");
        }
        if (hp > 0 || mp > 0) AudioManager.Instance.PlaySfx(RestSfxId);
    }

    private void RefreshLog()
    {
        logLabel.Text = string.Join('\n', _logLines);
    }

    private static Color ResolveSkillColor(BattleSkillCastInfo skill) =>
        skill.IsLegend
            ? SpecialColor
            : skill.ResolvedSkillKind switch
            {
                SkillKind.Special => SpecialColor,
                SkillKind.Internal => ManaColor,
                _ => InfoColor,
            };

    private static Color ResolveFloatTextColor(BattleFloatTextStyle style) => style switch
    {
        BattleFloatTextStyle.Positive => HealColor,
        BattleFloatTextStyle.Negative => StateColor,
        BattleFloatTextStyle.Status => StateColor,
        BattleFloatTextStyle.Info => InfoColor,
        BattleFloatTextStyle.Special => SpecialColor,
        _ => DamageColor,
    };

    private static string ResolveBuffName(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "状态";
        return GameRoot.ContentRepository.TryGetBuff(id, out var definition) ? definition.Name : id;
    }

    private static string ResolveItemName(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "物品";
        return GameRoot.ContentRepository.TryGetItem(id, out var definition) ? definition.Name : id;
    }

    private static string ResolveTalentName(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "天赋";
        return GameRoot.ContentRepository.TryGetTalent(id, out var definition) ? definition.Name : id;
    }
}
