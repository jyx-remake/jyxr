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
            AddStatsItemUseEffectDefinition addStats => string.Join("、", addStats.Values.Select(static entry =>
                $"{FormatterTextCn.GetStatNameCn(entry.Key)} {entry.Value:+0;-0;0}")),
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
            GrantTalentItemUseEffectDefinition talent => FormatGrantTalentCn(talent, contentRepository),
            GrantTitleItemUseEffectDefinition title => FormatGrantTitleCn(title, contentRepository),
            SetPortraitItemUseEffectDefinition => "改变头像",
            ClearBuffsItemUseEffectDefinition => "清除自身所有状态",
            RandomItemItemUseEffectDefinition randomItem => FormatRandomItemCn(randomItem, contentRepository),
            SetGenderItemUseEffectDefinition setGender =>
                $"性别变为{FormatterTextCn.GetGenderNameCn(setGender.Gender)}",
            ReduceMaxResourceRatioItemUseEffectDefinition reduction =>
                $"{FormatterTextCn.GetStatNameCn(reduction.StatId)}减少 {FormatterTextCn.FormatPercent(reduction.Ratio)}",
            RunStoryItemUseEffectDefinition => "触发剧情效果",
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
        => $"解毒：降低中毒等级 {detoxify.Values![0]}，缩短持续时间 {detoxify.Values[1]} 回合";

    private static string FormatGrantTalentCn(
        GrantTalentItemUseEffectDefinition grantTalent,
        IContentRepository contentRepository)
    {
        if (!contentRepository.TryGetTalent(grantTalent.TalentId, out var talent))
        {
            return $"获得天赋「{grantTalent.TalentId}」";
        }

        var line = $"获得天赋「{talent.Name}」";
        return string.IsNullOrWhiteSpace(talent.Description)
            ? line
            : $"{line}\n{talent.Description.Trim()}";
    }

    private static string FormatGrantSkillCn(string kind, string skillName, int? level) =>
        level is null
            ? $"学会{kind}「{skillName}」"
            : $"学会{kind}「{skillName}」（{level.Value}级）";

    private static string FormatGrantTitleCn(
        GrantTitleItemUseEffectDefinition grantTitle,
        IContentRepository contentRepository) =>
        contentRepository.TryGetCharacterTitle(grantTitle.TitleId, out var title)
            ? $"获得称号「{title.Name}」"
            : $"获得称号「{grantTitle.TitleId}」";

    private static string FormatRandomItemCn(
        RandomItemItemUseEffectDefinition randomItem,
        IContentRepository contentRepository)
    {
        var entries = randomItem.Items.Select(entry =>
        {
            var name = contentRepository.TryGetItem(entry.ItemId, out var item)
                ? item.Name
                : entry.ItemId;
            return $"{name}×{entry.Quantity}";
        });
        return $"随机获得：{string.Join(" / ", entries)}";
    }
}
