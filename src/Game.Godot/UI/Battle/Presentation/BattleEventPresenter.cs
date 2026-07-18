using Game.Core.Battle;
using Game.Core.Model.Skills;
using Game.Godot.Audio;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleEventPresenter(
    BattleBoardView board,
    BattleLogDrawer logDrawer,
    Func<BattleState?> stateProvider,
    Func<BattleMessage, bool> schedule)
{
    private const string RestSfxId = "音效.休息";

    public void Clear() => logDrawer.Clear();

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
                board.PlayFloatText(cue.UnitId, cue.FloatText.Text, cue.FloatText.Style);
            return;
        }

        if (message is not BattleFact fact) return;
        var unitName = state.TryGetUnit(fact.UnitId)?.Character.Name ?? fact.UnitId;
        switch (fact.Kind)
        {
            case BattleFactKind.SkillCast when fact.SkillCast is { } skill:
                board.PlayFloatText(fact.UnitId, skill.ResolvedSkillName, ResolveSkillStyle(skill));
                AppendLog(skill.IsLegend
                    ? $"{unitName} 施展奥义【{skill.ResolvedSkillName}】。"
                    : $"{unitName} 施展【{skill.ResolvedSkillName}】。");
                break;
            case BattleFactKind.SkillCooldownsReset:
                board.PlayFloatText(fact.UnitId, "冷却重置", BattleFloatTextStyle.Beneficial);
                AppendLog($"{unitName} 的天赋【{ResolveTalentName(fact.Detail)}】发动，所有技能冷却重置。");
                break;
            case BattleFactKind.Damaged:
                PresentDamage(fact, unitName);
                break;
            case BattleFactKind.BuffApplied:
                var applied = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, applied, ResolveBuffStyle(fact.Detail));
                AppendLog($"{unitName} 获得状态【{applied}】。");
                break;
            case BattleFactKind.BuffResisted:
                var resisted = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, "抵抗", BattleFloatTextStyle.Beneficial);
                AppendLog($"{unitName} 抵抗了状态【{resisted}】。");
                break;
            case BattleFactKind.BuffRemoved:
                board.PlayFloatText(fact.UnitId, $"{ResolveBuffName(fact.Detail)}解除", BattleFloatTextStyle.Beneficial);
                break;
            case BattleFactKind.Healed:
                PresentResourceChange(fact, BattleFloatTextStyle.Recovery, "+");
                break;
            case BattleFactKind.Lifesteal when fact.Lifesteal is { Amount: > 0 } lifesteal:
                board.PlayFloatText(fact.UnitId, $"吸血{lifesteal.Amount}", BattleFloatTextStyle.Recovery);
                AppendLog($"{unitName} 吸取了 {lifesteal.Amount} 点生命。");
                break;
            case BattleFactKind.MpDamaged:
                PresentResourceChange(fact, BattleFloatTextStyle.Mana, "-");
                break;
            case BattleFactKind.MpRecovered:
                PresentResourceChange(fact, BattleFloatTextStyle.Mana, "+");
                break;
            case BattleFactKind.RageChanged:
                PresentSignedResourceChange(fact, BattleFloatTextStyle.Energy, "怒气");
                break;
            case BattleFactKind.ActionGaugeChanged:
                PresentActionGaugeChange(fact);
                break;
            case BattleFactKind.Rested:
                PresentRest(fact, unitName);
                break;
            case BattleFactKind.ItemUsed:
                board.PlayFloatText(fact.UnitId, ResolveItemName(fact.Detail), BattleFloatTextStyle.Normal);
                break;
            case BattleFactKind.ActionSkipped:
                var reason = ResolveBuffName(fact.Detail);
                board.PlayFloatText(fact.UnitId, $"{reason}中", BattleFloatTextStyle.Harmful);
                AppendLog($"{unitName} 因【{reason}】无法行动。");
                break;
            case BattleFactKind.SkillLeveledUp when fact.SkillExperience is { } experience:
                board.PlayFloatText(
                    fact.UnitId,
                    experience.SkillKind == SkillKind.Internal
                        ? $"{experience.SkillName}等级提升"
                        : $"{experience.SkillName}等级升级",
                    BattleFloatTextStyle.Recovery);
                break;
            case BattleFactKind.CharacterLeveledUp when fact.CharacterExperience is not null:
                board.PlayFloatText(fact.UnitId, "角色等级提升", BattleFloatTextStyle.Recovery);
                break;
            case BattleFactKind.DefeatPrevented:
                AppendLog($"{unitName} 的天赋【{ResolveTalentName(fact.Detail)}】发动，避免了被击败。");
                break;
        }
    }

    public void AppendLog(string text)
    {
        logDrawer.Append(text);
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
            critical ? BattleFloatTextStyle.Critical : BattleFloatTextStyle.Normal);
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
            board.PlayFloatText(fact.UnitId, $"+{hp}", BattleFloatTextStyle.Recovery);
            AppendLog($"{unitName}回复生命值{hp}");
        }
        if (mp > 0)
        {
            board.PlayFloatText(fact.UnitId, $"+{mp}", BattleFloatTextStyle.Mana);
            AppendLog($"{unitName}回复内力{mp}");
        }
        if (hp > 0 || mp > 0) AudioManager.Instance.PlaySfx(RestSfxId);
    }

    private static BattleFloatTextStyle ResolveSkillStyle(BattleSkillCastInfo skill) =>
        skill.IsLegend
            ? BattleFloatTextStyle.Special
            : skill.ResolvedSkillKind switch
            {
                SkillKind.Special => BattleFloatTextStyle.Special,
                SkillKind.Internal => BattleFloatTextStyle.Mana,
                _ => BattleFloatTextStyle.Normal,
            };

    private void PresentResourceChange(BattleFact fact, BattleFloatTextStyle style, string prefix)
    {
        if (TryResolveAmount(fact.Detail, out var amount) && amount != 0)
            board.PlayFloatText(fact.UnitId, $"{prefix}{Math.Abs(amount)}", style);
    }

    private void PresentSignedResourceChange(BattleFact fact, BattleFloatTextStyle style, string resourceName)
    {
        if (TryResolveAmount(fact.Detail, out var amount) && amount != 0)
            board.PlayFloatText(fact.UnitId, $"{resourceName}{amount:+#;-#;0}", style);
    }

    private void PresentActionGaugeChange(BattleFact fact)
    {
        if (!double.TryParse(
                fact.Detail,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount) || Math.Abs(amount) <= double.Epsilon)
        {
            return;
        }

        board.PlayFloatText(
            fact.UnitId,
            $"集气{amount:+0.#;-0.#;0}",
            BattleFloatTextStyle.Energy);
    }

    private static bool TryResolveAmount(string? detail, out int amount)
    {
        var value = string.IsNullOrWhiteSpace(detail)
            ? null
            : detail[(detail.LastIndexOf(':') + 1)..];
        return int.TryParse(value, out amount);
    }

    private static BattleFloatTextStyle ResolveBuffStyle(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        GameRoot.ContentRepository.TryGetBuff(id, out var definition) &&
        !definition.IsDebuff
            ? BattleFloatTextStyle.Beneficial
            : BattleFloatTextStyle.Harmful;

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
