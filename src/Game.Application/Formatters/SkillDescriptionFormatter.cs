using System.Globalization;
using System.Text;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Application.Formatters;

public static class SkillDescriptionFormatter
{
    public static string FormatBbCodeCn(SkillInstance skill, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return skill switch
        {
            ExternalSkillInstance externalSkill => FormatExternalSkillBbCodeCn(externalSkill, contentRepository),
            InternalSkillInstance internalSkill => FormatInternalSkillBbCodeCn(internalSkill, contentRepository),
            FormSkillInstance formSkill => FormatFormSkillBbCodeCn(formSkill),
            LegendSkillInstance legendSkill => FormatLegendSkillBbCodeCn(legendSkill, contentRepository),
            SpecialSkillInstance specialSkill => FormatSpecialSkillBbCodeCn(specialSkill),
            _ => throw new NotSupportedException($"Unsupported skill type '{skill.GetType().Name}'.")
        };
    }

    private static string FormatExternalSkillBbCodeCn(
        ExternalSkillInstance skill,
        IContentRepository contentRepository)
    {
        var builder = new StringBuilder();
        AppendDescription(builder, skill.Description);
        AppendLine(builder, $"等级 {skill.Level}{FormatMaxLevel(skill.MaxLevel)}");
        AppendLine(builder, $"经验 {skill.Exp}/{skill.LevelUpExp}");
        AppendBaseCombatLines(builder, skill);
        AppendLine(builder, FormatAffinityBbCodeCn(skill.IsHarmony, skill.Affinity));
        AppendBuffLines(builder, skill.Buffs);
        AppendPassiveAffixLines(builder, skill.Definition.Affixes, skill.Level, skill.MaxLevel, contentRepository);
        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatInternalSkillBbCodeCn(
        InternalSkillInstance skill,
        IContentRepository contentRepository)
    {
        var builder = new StringBuilder();
        AppendDescription(builder, skill.Description);
        AppendLine(builder, $"等级 {skill.Level}{FormatMaxLevel(skill.MaxLevel)}");
        AppendLine(builder, $"经验 {skill.Exp}/{skill.LevelUpExp}");
        AppendRedLine(builder, $"+攻击 {FormatPercent(skill.AttackRatio)}");
        AppendGreenLine(builder, $"+防御 {FormatPercent(skill.DefenceRatio)}");
        AppendYellowLine(builder, $"+爆发 {FormatPercent(skill.CriticalRatio)}");
        AppendCyanLine(builder, $"阴适性 {skill.Yin}");
        AppendYellowLine(builder, $"阳适性 {skill.Yang}");
        AppendPassiveAffixLines(builder, skill.Definition.Affixes, skill.Level, skill.MaxLevel, contentRepository);
        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatFormSkillBbCodeCn(FormSkillInstance skill)
    {
        var builder = new StringBuilder();
        AppendLine(builder, $"{Colorize("white", "所属武学")} {Colorize("red", skill.SourceSkillName)}");
        AppendLine(builder, $"{Colorize("white", "招式解锁等级")} {Colorize("red", skill.Definition.UnlockLevel.ToString(CultureInfo.InvariantCulture))}");

        if (!string.IsNullOrWhiteSpace(skill.Description))
        {
            builder.Append('\n');
            AppendLine(builder, skill.Description);
        }

        AppendBaseCombatLines(builder, skill);
        AppendBuffLines(builder, skill.Buffs);
        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatLegendSkillBbCodeCn(
        LegendSkillInstance skill,
        IContentRepository contentRepository)
    {
        var builder = new StringBuilder();
        AppendLine(builder, $"{Colorize("white", "所属武学")} {Colorize("red", skill.Parent.Name)}");
        AppendLine(builder, $"{Colorize("white", "绝技解锁等级")} {Colorize("red", skill.Definition.RequiredLevel.ToString(CultureInfo.InvariantCulture))}");

        if (!string.IsNullOrWhiteSpace(skill.Description))
        {
            builder.Append('\n');
            AppendLine(builder, skill.Description);
        }

        AppendRedLine(builder, $"触发概率 {FormatPercent(skill.Definition.Probability)}");
        AppendBaseCombatLines(builder, skill);
        AppendLegendConditionLines(builder, skill.Definition.Conditions, contentRepository);
        AppendBuffLines(builder, skill.Buffs);
        return builder.ToString().TrimEnd('\n');
    }

    private static string FormatSpecialSkillBbCodeCn(SpecialSkillInstance skill)
    {
        var builder = new StringBuilder();
        AppendDescription(builder, skill.Description);
        AppendBaseCombatLines(builder, skill);
        AppendBuffLines(builder, skill.Buffs);
        AppendSpecialSkillEffectLines(builder, skill.Definition.Effects);
        return builder.ToString().TrimEnd('\n');
    }

    private static void AppendBaseCombatLines(StringBuilder builder, SkillInstance skill)
    {
        if (skill.Power > 0)
        {
            AppendRedLine(builder, $"威力 {FormatNumber(skill.Power)}");
        }

        AppendLine(builder, $"覆盖类型 {FormatImpactTypeCn(skill.ImpactType)}");
        AppendLine(builder, $"覆盖范围 {skill.ImpactSize}");
        AppendLine(builder, $"施展范围 {skill.CastSize}");

        if (skill.MpCost > 0)
        {
            AppendCyanLine(builder, $"消耗内力 {skill.MpCost}");
        }

        if (skill.RageCost > 0)
        {
            AppendYellowLine(builder, $"消耗怒气 {skill.RageCost}");
        }

        if (skill.Cooldown > 0 || skill.CurrentCooldown > 0)
        {
            var color = skill.CurrentCooldown == 0 ? "green" : "red";
            AppendLine(builder, Colorize(color, $"技能CD {skill.CurrentCooldown}/{skill.Cooldown}"));
        }
    }

    private static void AppendDescription(StringBuilder builder, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        AppendLine(builder, description);
    }

    private static void AppendBuffLines(StringBuilder builder, IReadOnlyList<SkillBuffDefinition> buffs)
    {
        foreach (var buff in buffs)
        {
            AppendLine(builder, FormatBuffBbCode(buff));
        }
    }

    private static void AppendSpecialSkillEffectLines(
        StringBuilder builder,
        IReadOnlyList<BattleEffectDefinition>? effects)
    {
        foreach (var effect in effects ?? [])
        {
            AppendLine(builder, effect switch
            {
                RemoveBuffBattleEffectDefinition removeBuff =>
                    FormatTargetedEffectCn($"移除状态「{removeBuff.Buff.Name}」", removeBuff.Target),
                RemoveNegativeBuffsBattleEffectDefinition removeNegativeBuffs =>
                    FormatTargetedEffectCn("移除异常状态", removeNegativeBuffs.Target),
                RemovePositiveBuffsBattleEffectDefinition removePositiveBuffs =>
                    FormatTargetedEffectCn("移除增益状态", removePositiveBuffs.Target),
                AddRageBattleEffectDefinition addRage =>
                    FormatTargetedEffectCn($"怒气 +{addRage.Value}", addRage.Target),
                SetRageBattleEffectDefinition setRage =>
                    FormatTargetedEffectCn($"怒气设为 {setRage.Value}", setRage.Target),
                SetActionGaugeBattleEffectDefinition setActionGauge =>
                    FormatTargetedEffectCn($"行动值设为 {setActionGauge.Value}", setActionGauge.Target),
                AddHpBattleEffectDefinition addHp =>
                    FormatTargetedEffectCn($"恢复气血 {addHp.Value}", addHp.Target),
                AddMpBattleEffectDefinition addMp =>
                    FormatTargetedEffectCn($"恢复内力 {addMp.Value}", addMp.Target),
                ApplyBuffBattleEffectDefinition addBuff =>
                    FormatTargetedEffectCn($"附加状态「{addBuff.Buff.Name}」", addBuff.Target) +
                    $"（等级 {addBuff.Level}，持续 {addBuff.Duration} 回合）",
                _ => throw new NotSupportedException($"Unsupported special skill effect '{effect.GetType().Name}'.")
            });
        }
    }

    private static string FormatTargetedEffectCn(string text, BattleTargetSelectorDefinition target) =>
        target switch
        {
            SelfBattleTargetSelectorDefinition => $"对自身{text}",
            SourceBattleTargetSelectorDefinition => $"对施法者{text}",
            TargetBattleTargetSelectorDefinition => $"对命中目标{text}",
            AllAlliesBattleTargetSelectorDefinition => $"对全体友军{text}",
            AllEnemiesBattleTargetSelectorDefinition => $"对全体敌军{text}",
            NearbyAlliesBattleTargetSelectorDefinition nearbyAllies =>
                $"对{nearbyAllies.Radius}格内友军{text}",
            _ => text
        };

    private static void AppendPassiveAffixLines(
        StringBuilder builder,
        IReadOnlyList<SkillAffixDefinition> affixes,
        int level,
        int? maxLevel,
        IContentRepository contentRepository)
    {
        if (affixes.Count == 0)
        {
            return;
        }

        builder.Append('\n');
        builder.Append("被动增益：\n");

        foreach (var affix in affixes)
        {
            AppendLine(builder, FormatPassiveAffixBbCode(affix, level, maxLevel, contentRepository));
        }
    }

    private static void AppendLegendConditionLines(
        StringBuilder builder,
        IReadOnlyList<LegendSkillConditionDefinition> conditions,
        IContentRepository contentRepository)
    {
        if (conditions.Count == 0)
        {
            return;
        }

        builder.Append('\n');
        builder.Append("触发条件：\n");

        foreach (var condition in conditions)
        {
            AppendBlackLine(builder, FormatLegendConditionCn(condition, contentRepository));
        }
    }

    private static string FormatPassiveAffixBbCode(
        SkillAffixDefinition affix,
        int level,
        int? maxLevel,
        IContentRepository contentRepository)
    {
        if (level >= affix.MinimumLevel)
        {
            return Colorize("green", $"(√)({affix.MinimumLevel}级解锁){FormatPassiveAffixBody(affix, contentRepository)}");
        }

        if (maxLevel is not null && maxLevel.Value < affix.MinimumLevel)
        {
            return Colorize("red", $"(×)({affix.MinimumLevel}级解锁)???");
        }

        return Colorize("red", $"(×)({affix.MinimumLevel}级解锁){FormatPassiveAffixBody(affix, contentRepository)}");
    }

    private static string FormatPassiveAffixBody(SkillAffixDefinition affix, IContentRepository contentRepository)
    {
        var effectText = AffixFormatter.FormatCn(affix.Effect, contentRepository);
        return affix.RequiresEquippedInternalSkill
            ? $"装备生效：{effectText}"
            : effectText;
    }

    private static string FormatBuffBbCode(SkillBuffDefinition buff)
    {
        var parts = new List<string>
        {
            Colorize("yellow", $"特效：{ResolveBuffName(buff)}({buff.Level})")
        };

        if (buff.Duration > 0)
        {
            parts.Add(Colorize("yellow", $"持续{buff.Duration}回合"));
        }

        if (buff.Chance >= 100)
        {
            parts.Add(Colorize("red", "必定命中"));
        }
        else if (buff.Chance > 0)
        {
            parts.Add(Colorize("yellow", $"命中概率:{buff.Chance}%"));
        }

        return string.Join(' ', parts);
    }

    private static string ResolveBuffName(SkillBuffDefinition buff) =>
        buff.Buff?.Name ?? buff.Id;

    private static string FormatLegendConditionCn(
        LegendSkillConditionDefinition condition,
        IContentRepository contentRepository) =>
        condition switch
        {
            RequiredExternalSkillLevelLegendConditionDefinition externalSkill =>
                $"需要外功「{ResolveExternalSkillName(externalSkill.TargetId, contentRepository)}」达到{externalSkill.Level}级",
            RequiredInternalSkillLevelLegendConditionDefinition internalSkill =>
                $"需要内功「{ResolveInternalSkillName(internalSkill.TargetId, contentRepository)}」达到{internalSkill.Level}级",
            RequiredSpecialSkillLegendConditionDefinition specialSkill =>
                $"需要特殊技能「{ResolveSpecialSkillName(specialSkill.TargetId, contentRepository)}」",
            RequiredTalentLegendConditionDefinition talent =>
                $"需要天赋「{ResolveTalentName(talent.TargetId, contentRepository)}」",
            _ => throw new NotSupportedException($"Unsupported legend condition type '{condition.GetType().Name}'.")
        };

    private static string ResolveExternalSkillName(string id, IContentRepository contentRepository)
        => contentRepository.TryGetExternalSkill(id, out var skill)
            ? skill.Name
            : id;

    private static string ResolveInternalSkillName(string id, IContentRepository contentRepository)
        => contentRepository.TryGetInternalSkill(id, out var skill)
            ? skill.Name
            : id;

    private static string ResolveSpecialSkillName(string id, IContentRepository contentRepository)
        => contentRepository.TryGetSpecialSkill(id, out var skill)
            ? skill.Name
            : id;

    private static string ResolveTalentName(string id, IContentRepository contentRepository)
        => contentRepository.TryGetTalent(id, out var talent)
            ? talent.Name
            : id;

    private static string FormatImpactTypeCn(SkillImpactType impactType) =>
        impactType switch
        {
            SkillImpactType.Single => "点攻击",
            SkillImpactType.Plus => "十字攻击",
            SkillImpactType.Star => "米字攻击",
            SkillImpactType.Line => "直线攻击",
            SkillImpactType.Square => "面攻击",
            SkillImpactType.Fan => "扇形攻击",
            SkillImpactType.Ring => "环状攻击",
            SkillImpactType.X => "对角线攻击",
            SkillImpactType.Cleave => "身前攻击",
            _ => throw new ArgumentOutOfRangeException(nameof(impactType), impactType, null),
        };

    private static string FormatAffinityBbCodeCn(bool isHarmony, double affinity)
    {
        if (isHarmony)
        {
            return Colorize("green", "适性:阴阳调和");
        }

        if (Math.Abs(affinity) <= double.Epsilon)
        {
            return Colorize("black", "适性:无");
        }

        if (affinity > 0)
        {
            return Colorize("yellow", $"适性:阳{FormatUnsignedPercent(affinity)}");
        }

        return Colorize("cyan", $"适性:阴{FormatUnsignedPercent(Math.Abs(affinity))}");
    }

    private static string FormatPercent(double value) =>
        $"{FormatUnsignedPercent(value)}";

    private static string FormatUnsignedPercent(double value) =>
        $"{FormatNumber(value * 100d)}%";

    private static string FormatNumber(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero).ToString("0.######", CultureInfo.InvariantCulture);

    private static string FormatMaxLevel(int? maxLevel) =>
        maxLevel is null ? string.Empty : $"/{maxLevel.Value}";

    private static void AppendBlackLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("black", text));

    private static void AppendRedLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("red", text));

    private static void AppendGreenLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("green", text));

    private static void AppendYellowLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("yellow", text));

    private static void AppendCyanLine(StringBuilder builder, string text) =>
        AppendLine(builder, Colorize("cyan", text));

    private static void AppendLine(StringBuilder builder, string text)
    {
        builder.Append(text);
        builder.Append('\n');
    }

    private static string Colorize(string color, string text) =>
        $"[color={color}]{text}[/color]";
}
