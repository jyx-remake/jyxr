namespace Game.Core.Battle;

internal sealed partial class BattleEventRuntimeSession
{
    private enum StepControl
    {
        Continue,
        Jump,
        Return,
        Terminate,
    }

    private sealed record StepResult(BattleEvent? Event, StepControl Control, string? Target)
    {
        public bool IsControl => Control != StepControl.Continue;

        public static StepResult FromEvent(BattleEvent battleEvent) => new(battleEvent, StepControl.Continue, null);

        public static StepResult Jump(string jumpTarget) => new(null, StepControl.Jump, jumpTarget);

        public static StepResult Return() => new(null, StepControl.Return, null);

        public static StepResult Terminate() => new(null, StepControl.Terminate, null);
    }
}
