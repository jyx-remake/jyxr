using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class BattleAiPolicyResolver : IBattleAiPolicyResolver
{
    private readonly IBattleAiPolicy _basicPolicy = new BasicBattleAiPolicy();
    private readonly IBattleAiPolicy _trainingPolicy;
    private readonly IBattleAiPolicy _attackOnlyPolicy = new AttackOnlyBattleAiPolicy();
    private readonly IBattleAiPolicy _restOnlyPolicy = new RestOnlyBattleAiPolicy();

    public BattleAiPolicyResolver(Func<SkillInstance, int> skillMaxLevelResolver)
    {
        _trainingPolicy = new TrainingBattleAiPolicy(skillMaxLevelResolver);
    }

    public IBattleAiPolicy Resolve(BattleAiType aiType) =>
        aiType switch
        {
            BattleAiType.Training => _trainingPolicy,
            BattleAiType.AttackOnly => _attackOnlyPolicy,
            BattleAiType.RestOnly => _restOnlyPolicy,
            _ => _basicPolicy,
        };
}
