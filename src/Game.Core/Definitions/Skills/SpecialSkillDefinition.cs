using Game.Core.Abstractions;
using Game.Core.Battle;

namespace Game.Core.Definitions.Skills;

public sealed record SpecialSkillDefinition(
    string Id,
    string Name,
    string Description,
    string Icon,
    int Cooldown,
    SkillCostDefinition Cost,
    SkillTargetingDefinition? Targeting,
    string Animation,
    string Audio,
    BattleSpeechDefinition? Speech,
    IReadOnlyList<SkillBuffDefinition> Buffs,
    IReadOnlyList<BattleEffectDefinition>? Effects = null)
{
    public void Resolve(IContentRepository contentRepository)
    {
        foreach (var buff in Buffs)
        {
            buff.Resolve(contentRepository);
        }

        foreach (var effect in Effects ?? [])
        {
            effect.Resolve(contentRepository);
        }
    }
}
