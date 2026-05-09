using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public interface IBattleSkillAiScorer
{
    bool CanScore(SkillInstance skill);

    BattleSkillAiEvaluation Score(BattleSkillAiContext context);
}
