namespace Game.Core.Story;

internal sealed partial class StoryRuntimeSession
{
    private enum StepControl
    {
        Continue,
        Jump,
        Return,
        Terminate,
    }

    private sealed record StepResult(StoryEvent? Event, StepControl Control, string? Target)
    {
        public bool IsControl => Control != StepControl.Continue;

        public static StepResult FromEvent(StoryEvent storyEvent) => new(storyEvent, StepControl.Continue, null);

        public static StepResult Jump(string jumpTarget) => new(null, StepControl.Jump, jumpTarget);

        public static StepResult Return() => new(null, StepControl.Return, null);

        public static StepResult Terminate() => new(null, StepControl.Terminate, null);
    }
}
