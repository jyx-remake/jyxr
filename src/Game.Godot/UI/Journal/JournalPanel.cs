using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class JournalPanel : JyPanel
{
    [Export]
    public PackedScene JournalEntryScene { get; set; } = null!;

    private VBoxContainer _entryContainer = null!;
    private Label _emptyLabel = null!;

    public override void _Ready()
    {
        base._Ready();
        _entryContainer = GetNode<VBoxContainer>("%EntryContainer");
        _emptyLabel = GetNode<Label>("%EmptyLabel");
        Refresh();
    }

    private void Refresh()
    {
        ClearEntries();

        var entries = Game.State.Journal.Entries;
        if (entries.Count == 0)
        {
            _emptyLabel.Visible = true;
            return;
        }

        _emptyLabel.Visible = false;
        foreach (var entry in entries)
        {
            _entryContainer.AddChild(CreateEntry(entry));
        }
    }

    private JournalEntryLabel CreateEntry(JournalEntry entry)
    {
        if (JournalEntryScene is null)
        {
            throw new InvalidOperationException("JournalEntryScene is not assigned.");
        }

        var instance = JournalEntryScene.Instantiate();
        if (instance is not JournalEntryLabel entryLabel)
        {
            instance.QueueFree();
            throw new InvalidOperationException("Journal entry scene root must be JournalEntryLabel.");
        }

        entryLabel.Setup(entry);
        return entryLabel;
    }

    private void ClearEntries()
    {
        foreach (var child in _entryContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
