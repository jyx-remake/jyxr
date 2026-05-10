using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Affix;

namespace Game.Core.Battle;

public enum BattleSpeechSpeaker
{
    [JsonStringEnumMemberName("owner")]
    Owner,
    [JsonStringEnumMemberName("source")]
    Source,
    [JsonStringEnumMemberName("target")]
    Target
}

public sealed record BattleSpeechDefinition
{
    public BattleSpeechSpeaker Speaker { get; init; } = BattleSpeechSpeaker.Owner;
    public required IReadOnlyList<string> Lines { get; init; }
    public double Chance { get; init; } = 1d;
}

public static class BattleSpeechRuntime
{
    public static string? TryPickLine(BattleSpeechDefinition? speech, IRandomService random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (speech is null || speech.Lines.Count == 0)
        {
            return null;
        }

        if (!random.RollChance(speech.Chance))
        {
            return null;
        }

        var line = speech.Lines.Count == 1
            ? speech.Lines[0]
            : speech.Lines[random.Next(0, speech.Lines.Count)];
        return string.IsNullOrWhiteSpace(line)
            ? null
            : line;
    }

    public static void TryEmit(
        BattleState state,
        BattleUnit? speaker,
        string? text,
        HookTiming? timing = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (speaker is null || !speaker.IsAlive || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (state.CurrentAction is not null &&
            !state.CurrentAction.TryRegisterSpeech(speaker.Id))
        {
            return;
        }

        state.AddEvent(new BattleEvent(
            BattleEventKind.SpeechRequested,
            speaker.Id,
            timing,
            Speech: new BattleSpeechCue(text)));
    }

    public static string? FormatText(
        string? text,
        BattleUnit? owner = null,
        BattleUnit? source = null,
        BattleUnit? target = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text
            .Replace("{owner}", owner?.Character.Name ?? string.Empty, StringComparison.Ordinal)
            .Replace("{source}", source?.Character.Name ?? string.Empty, StringComparison.Ordinal)
            .Replace("{target}", target?.Character.Name ?? string.Empty, StringComparison.Ordinal);
    }
}
