using Game.Application.Mods;
using Game.Application;
using Game.Core.Battle;
using Game.Godot;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless validation for every generated ordinary battle definition.
/// </summary>
public partial class XmjhBattleSmoke : SceneTree
{
    public override void _Initialize() => CallDeferred(nameof(RunSmoke));

    private void RunSmoke()
    {
        try
        {
            var projectRoot = ProjectSettings.GlobalizePath("res://");
            var dataRoot = ProjectDataRoot.FromPath(projectRoot);
            var mods = new ModRegistry(dataRoot).DiscoverMods();
            var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);
            GameRuntimeBootstrap.Initialize(loadout, this);

            var success = 0;
            var failures = new List<string>();
            var conditional = 0;
            var noEnemy = 0;
            var selectedCharacterIds = Game.State.Party.Members.Select(character => character.Id).ToArray();
            foreach (var battle in Game.ContentRepository.GetBattles())
            {
                try
                {
                    var state = Game.BattleService.BuildBattleState(new OrdinaryBattleRequest(battle.Id, selectedCharacterIds));
                    if (!state.Units.Any(unit => unit.Team == Game.Config.BattlePlayerTeam))
                    {
                        failures.Add($"{battle.Id}: missing player unit ({state.Units.Count})");
                    }
                    else
                    {
                        success++;
                        if (!state.Units.Any(unit => unit.Team != Game.Config.BattlePlayerTeam))
                        {
                            noEnemy++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (exception.Message.StartsWith("Battle '", StringComparison.Ordinal) &&
                        (exception.Message.Contains("requires character", StringComparison.Ordinal) ||
                         exception.Message.Contains("forbids character", StringComparison.Ordinal)))
                    {
                        conditional++;
                    }
                    else
                    {
                        failures.Add($"{battle.Id}: {exception.Message}");
                    }
                }
            }

            GD.Print($"XMJH_BATTLE_SMOKE party={string.Join(",", selectedCharacterIds)} total={Game.ContentRepository.GetBattles().Count} success={success} conditional={conditional} noEnemy={noEnemy} failures={failures.Count}");
            foreach (var failure in failures.Take(20))
            {
                GD.PrintErr($"XMJH_BATTLE_SMOKE_FAILURE {failure}");
            }

            Quit(failures.Count == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"XMJH_BATTLE_SMOKE_FAILED {exception}");
            Quit(1);
        }
    }
}
