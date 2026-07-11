using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record ToadMasteryAttackBattleEffectParameters(
    string SkillNameFragment,
    string InternalSkillId,
    double SkillAttackBonus,
    double InternalSkillAttackBonus,
    double SpeechChance,
    IReadOnlyList<string> SkillSpeechLines,
    IReadOnlyList<string> InternalSkillSpeechLines);

internal sealed class ToadMasteryAttackBattleEffectHandler
    : CustomBattleEffectHandler<ToadMasteryAttackBattleEffectParameters, IDamageCalculationEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(ToadMasteryAttackBattleEffectParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.SkillNameFragment);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.InternalSkillId);
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.SkillAttackBonus);
        ArgumentOutOfRangeException.ThrowIfNegative(parameters.InternalSkillAttackBonus);
        if (parameters.SpeechChance is < 0d or > 1d)
        {
            throw new InvalidOperationException("Toad mastery speech chance must be between 0 and 1.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        ToadMasteryAttackBattleEffectParameters parameters)
    {
        double attackBonus;
        IReadOnlyList<string> speechLines;
        if (context.Skill?.Name.Contains(parameters.SkillNameFragment, StringComparison.Ordinal) == true)
        {
            attackBonus = parameters.SkillAttackBonus;
            speechLines = parameters.SkillSpeechLines;
        }
        else if (context.Unit.Character.GetInternalSkills().Any(skill =>
            skill.IsEquipped && string.Equals(
                skill.Definition.Id,
                parameters.InternalSkillId,
                StringComparison.Ordinal)))
        {
            attackBonus = parameters.InternalSkillAttackBonus;
            speechLines = parameters.InternalSkillSpeechLines;
        }
        else
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttack,
            ModifierOp.Add,
            attackBonus);

        var speech = BattleSpeechRuntime.TryPickLine(
            new BattleSpeechDefinition
            {
                Lines = speechLines,
                Chance = parameters.SpeechChance,
            },
            context.Random);
        if (speech is not null)
        {
            context.RequestSpeech(context.Unit, speech);
        }
    }
}
