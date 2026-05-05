using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class JournalEntryLabel : Label
{
    public void Setup(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Text = $"{ClockFormatter.FormatLogPrefixCn(ClockState.Restore(entry.Timestamp))}，{entry.Text}";
    }
}
