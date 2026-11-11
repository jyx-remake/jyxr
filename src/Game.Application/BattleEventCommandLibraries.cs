using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;
using Game.Core.Battle;

namespace Game.Application;

internal sealed class InventoryCurrencyBattleEventCommands
{
    private readonly GameSession _session;
    private readonly IRuntimeHost _host;
    private readonly BattleState _battleState;


    public InventoryCurrencyBattleEventCommands(GameSession session, IRuntimeHost host, BattleState battleState)
    {
        _session = session;
        _host = host;
        _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
    }

    [BattleEventCommand("change_units_mp")]
    public ValueTask ChangeUnit(
        IReadOnlyList<string> unitIds,
        IReadOnlyList<double> amounts,
        IReadOnlyList<string>? kinds = null,
        CancellationToken cancellationToken = default)
    {
        _session.BattleService.UnitsMpChange(_battleState, unitIds, amounts, kinds);

        return ValueTask.CompletedTask;
    }


}

internal sealed class BattleStateCommands
{


}

