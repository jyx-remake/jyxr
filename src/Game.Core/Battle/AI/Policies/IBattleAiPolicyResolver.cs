using Game.Core.Model;

namespace Game.Core.Battle;

public interface IBattleAiPolicyResolver
{
    IBattleAiPolicy Resolve(BattleAiType aiType);
}
