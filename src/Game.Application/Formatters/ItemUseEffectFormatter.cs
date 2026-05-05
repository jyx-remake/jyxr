using Game.Core.Abstractions;
using Game.Core.Definitions;

namespace Game.Application.Formatters;

public static class ItemUseEffectFormatter
{
    public static string FormatCn(ItemUseEffectDefinition useEffect, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(useEffect);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return useEffect switch
        {
            AddBuffItemUseEffectDefinition addBuff => FormatAddBuffCn(addBuff, contentRepository),
            AddRageItemUseEffectDefinition addRage => $"怒气 +{addRage.Value}",
            DetoxifyItemUseEffectDefinition detoxify => FormatDetoxifyCn(detoxify),
            AddMaxHpItemUseEffectDefinition addMaxHp => $"气血上限 +{addMaxHp.Value}",
            AddMaxMpItemUseEffectDefinition addMaxMp => $"内力上限 +{addMaxMp.Value}",
            AddHpItemUseEffectDefinition addHp => $"恢复气血 {addHp.Value}",
            AddMpItemUseEffectDefinition addMp => $"恢复内力 {addMp.Value}",
            AddHpPercentItemUseEffectDefinition addHpPercent => $"恢复气血 {addHpPercent.Value}%",
            AddMpPercentItemUseEffectDefinition addMpPercent => $"恢复内力 {addMpPercent.Value}%",
            GrantExternalSkillItemUseEffectDefinition externalSkill => FormatGrantSkillCn(
                "外功",
                FormatterTextCn.ResolveExternalSkillName(externalSkill.SkillId, contentRepository),
                externalSkill.Level),
            GrantInternalSkillItemUseEffectDefinition internalSkill => FormatGrantSkillCn(
                "内功",
                FormatterTextCn.ResolveInternalSkillName(internalSkill.SkillId, contentRepository),
                internalSkill.Level),
            GrantSpecialSkillItemUseEffectDefinition specialSkill =>
                $"学会特殊技能「{FormatterTextCn.ResolveSpecialSkillName(specialSkill.SkillId, contentRepository)}」",
            GrantTalentItemUseEffectDefinition talent =>
                $"获得天赋「{FormatterTextCn.ResolveTalentName(talent.TalentId, contentRepository)}」",
            _ => throw new NotSupportedException($"Unsupported item use effect type '{useEffect.GetType().Name}'.")
        };
    }

    public static IReadOnlyList<string> FormatLinesCn(
        IEnumerable<ItemUseEffectDefinition> useEffects,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(useEffects);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return useEffects.Select(useEffect => FormatCn(useEffect, contentRepository)).ToList();
    }

    private static string FormatAddBuffCn(AddBuffItemUseEffectDefinition addBuff, IContentRepository contentRepository)
    {
        var text = $"附加状态「{FormatterTextCn.ResolveBuffName(addBuff.BuffId, contentRepository)}」";
        return $"{text}（等级 {addBuff.Level}，持续 {addBuff.Duration} 回合）";
    }

    private static string FormatDetoxifyCn(DetoxifyItemUseEffectDefinition detoxify)
    {
        if (detoxify.Values is null || detoxify.Values.Count == 0)
        {
            return "解毒";
        }

        if (detoxify.Values.Count == 1)
        {
            return $"解毒等级 {detoxify.Values[0]}";
        }

        return $"解毒：等级 {detoxify.Values[0]}，持续 {detoxify.Values[1]}";
    }

    private static string FormatGrantSkillCn(string kind, string skillName, int? level) =>
        level is null
            ? $"学会{kind}「{skillName}」"
            : $"学会{kind}「{skillName}」（{level.Value}级）";
}
