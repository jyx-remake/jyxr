using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class StoryVariableResolver
{
    private readonly GameSession _session;

    public StoryVariableResolver(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private AdventureState Adventure => State.Adventure;

    public bool TryGetVariable(string name, out ExprValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        switch (name)
        {
            case "money":
            case "silver":
                value = ExprValue.FromNumber(State.Currency.Silver);
                return true;
            case "yuanbao":
            case "gold":
                value = ExprValue.FromNumber(State.Currency.Gold);
                return true;
            case "round":
                value = ExprValue.FromNumber(Adventure.Round);
                return true;
            case "game_mode":
                value = ExprValue.FromString(Adventure.GetModeId());
                return true;
            case "menpai":
                value = ExprValue.FromString(Adventure.SectId ?? string.Empty);
                return true;
            case "daode":
                value = ExprValue.FromNumber(Adventure.Morality);
                return true;
            case "haogan":
                value = ExprValue.FromNumber(Adventure.Favorability);
                return true;
            case "rank":
                value = ExprValue.FromNumber(Adventure.Rank);
                return true;
            default:
                value = default;
                return false;
        }
    }
}
