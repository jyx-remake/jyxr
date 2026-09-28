using Game.Application.Formatters;
using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class EquipmentRefinementService
{
    private const int CandidateCount = 8;
    private const int MaxAffixGroupCount = 5;
    private const string NoEquipmentStoryId = "洗练_没有装备";
    private const string InsufficientYuanbaoStoryId = "洗练元宝不够";
    private const string CancelOptionText = "不替换了";
    private const string ExitOptionText = "退出洗练";
    private const string SuccessEffectId = "音效.装备";

    private readonly GameSession _session;

    public EquipmentRefinementService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public async ValueTask<StoryCommandResult> RunAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        var state = State;
        var profile = _session.Profile;
        var equipmentEntries = state.Inventory.Entries
            .Where(static entry => entry.Definition is EquipmentDefinition definition && IsRefinableSlot(definition.SlotType))
            .ToArray();
        if (equipmentEntries.Length == 0)
        {
            return StoryCommandResult.Jump(NoEquipmentStoryId);
        }

        var selectedEntry = await SelectEquipmentAsync(host, equipmentEntries, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (selectedEntry is null)
        {
            return StoryCommandResult.None;
        }

        return await RunRefinementLoopAsync(host, state, profile, selectedEntry, cancellationToken);
    }

    private async ValueTask<StoryCommandResult> RunRefinementLoopAsync(
        IRuntimeHost host,
        GameState state,
        GameProfile profile,
        InventoryEntry entry,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var selection = new RefinementSelection(state, profile, entry,
                (entry as EquipmentInstanceInventoryEntry)?.Equipment.ExtraAffixes.ToArray() ?? []);
            if (!IsCurrentSelection(selection, cancellationToken))
            {
                return StoryCommandResult.None;
            }

            var affixGroups = EquipmentAffixGroups.Group(selection.Affixes);
            var operation = await SelectOperationAsync(host, affixGroups, cancellationToken);
            if (!IsCurrentSelection(selection, cancellationToken) || operation.Kind == RefinementOperationKind.Exit)
            {
                return StoryCommandResult.None;
            }

            if (!_session.ProfileService.CanSpendYuanbao(1))
            {
                return StoryCommandResult.Jump(InsufficientYuanbaoStoryId);
            }

            var candidates = GenerateCandidates((EquipmentDefinition)entry.Definition, affixGroups, state.Adventure.Round);
            if (candidates.Count == 0)
            {
                _session.Events.Publish(new ToastRequestedEvent("无可用新词条，未扣元宝。"));
                return StoryCommandResult.None;
            }
            _session.ProfileService.SpendYuanbao(1);

            var candidate = await SelectCandidateAsync(host, operation, candidates, cancellationToken);
            if (!IsCurrentSelection(selection, cancellationToken))
            {
                return StoryCommandResult.None;
            }
            if (candidate is null)
            {
                continue;
            }

            entry = ApplyOperation(selection, operation, candidate.Affixes);
            _session.Events.Publish(new InventoryChangedEvent());
            _session.Events.Publish(new ToastRequestedEvent(
                operation.Kind == RefinementOperationKind.Add ? "增加词条成功！" : "洗练成功！"));
            await host.PlayEffectAsync(SuccessEffectId, cancellationToken);
        }
    }

    private bool IsCurrentSelection(RefinementSelection selection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReferenceEquals(State, selection.State) && ReferenceEquals(_session.Profile, selection.Profile)
            && selection.State.Inventory.Entries.Contains(selection.Entry)
            && (selection.Entry is not EquipmentInstanceInventoryEntry equipmentEntry
                || equipmentEntry.Equipment.ExtraAffixes.SequenceEqual(selection.Affixes));
    }

    private async ValueTask<RefinementOperation> SelectOperationAsync(
        IRuntimeHost host, IReadOnlyList<EquipmentAffixGroup> groups, CancellationToken cancellationToken)
    {
        var operations = groups.Select(group => new RefinementOperation(
            RefinementOperationKind.Replace, FormatAffixGroup(group.Affixes), group)).ToList();
        var canAdd = groups.Count < MaxAffixGroupCount;
        if (canAdd)
        {
            operations.Add(new RefinementOperation(RefinementOperationKind.Add, "增加词条"));
        }
        operations.Add(new RefinementOperation(RefinementOperationKind.Exit, ExitOptionText));
        var index = await ChooseAsync(host, "主角",
            $"词条 {groups.Count}/{MaxAffixGroupCount}，{(canAdd ? "可替换或增加" : "已满，可替换")}。每次 1 元宝，放弃不退。",
            operations.Select(operation => operation.Text).ToArray(), cancellationToken);
        return operations[index];
    }

    private async ValueTask<GeneratedEquipmentAffixRoll?> SelectCandidateAsync(
        IRuntimeHost host, RefinementOperation operation,
        IReadOnlyList<GeneratedEquipmentAffixRoll> candidates, CancellationToken cancellationToken)
    {
        var cancelText = operation.Kind == RefinementOperationKind.Add
            ? "放弃增加" : $"{CancelOptionText}（{operation.Text}）";
        var index = await ChooseAsync(host, "主角", "选择新词条",
            candidates.Select(candidate => FormatAffixGroup(candidate.Affixes)).Append(cancelText).ToArray(),
            cancellationToken);
        return index == candidates.Count ? null : candidates[index];
    }

    private static InventoryEntry ApplyOperation(
        RefinementSelection selection, RefinementOperation operation, IReadOnlyList<AffixDefinition> newAffixes)
    {
        var affixes = selection.Affixes.ToList();
        switch (operation.Kind)
        {
            case RefinementOperationKind.Add when EquipmentAffixGroups.Count(affixes) < MaxAffixGroupCount:
                affixes.AddRange(newAffixes);
                break;
            case RefinementOperationKind.Replace when operation.Group is { } group:
                affixes.RemoveRange(group.StartIndex, group.Count);
                affixes.InsertRange(group.StartIndex, newAffixes);
                break;
            default:
                throw new InvalidOperationException("Invalid equipment refinement operation.");
        }

        if (selection.Entry is EquipmentInstanceInventoryEntry equipmentEntry)
        {
            equipmentEntry.Equipment.ReplaceExtraAffixes(affixes);
            return equipmentEntry;
        }

        var equipment = selection.State.EquipmentInstanceFactory.Create(
            (EquipmentDefinition)selection.Entry.Definition, affixes);
        selection.State.Inventory.RemoveItem(selection.Entry.Definition);
        return selection.State.Inventory.AddEquipmentInstance(equipment);
    }

    private sealed record RefinementSelection(
        GameState State, GameProfile Profile, InventoryEntry Entry, IReadOnlyList<AffixDefinition> Affixes);

    private enum RefinementOperationKind { Add, Replace, Exit }

    private sealed record RefinementOperation(
        RefinementOperationKind Kind, string Text, EquipmentAffixGroup? Group = null);

    private IReadOnlyList<GeneratedEquipmentAffixRoll> GenerateCandidates(
        EquipmentDefinition equipment,
        IReadOnlyList<EquipmentAffixGroup> currentAffixGroups,
        int round)
    {
        var candidates = new List<GeneratedEquipmentAffixRoll>(CandidateCount);
        var excludedGroups = currentAffixGroups.Select(static group => group.Affixes).ToArray();
        for (var candidateIndex = 0; candidateIndex < CandidateCount; candidateIndex++)
        {
            var candidate = EquipmentRandomAffixGenerator.TryGenerateSingleRoll(
                equipment,
                ContentRepository,
                round,
                _session.RandomService,
                excludedGroups);
            if (candidate is null)
            {
                break;
            }
            candidates.Add(candidate);
        }

        return candidates;
    }

    private async ValueTask<InventoryEntry?> SelectEquipmentAsync(
        IRuntimeHost host,
        IReadOnlyList<InventoryEntry> equipmentEntries,
        CancellationToken cancellationToken)
    {
        if (host is not IApplicationRuntimeHost applicationHost)
        {
            throw new InvalidOperationException("Equipment refinement requires an application runtime host.");
        }

        var selectedEntry = await applicationHost.SelectRefinementEquipmentAsync(equipmentEntries, cancellationToken);
        if (selectedEntry is null)
        {
            return null;
        }

        if (!equipmentEntries.Contains(selectedEntry))
        {
            throw new InvalidOperationException("Refinement equipment selection returned an entry outside the candidate list.");
        }

        return selectedEntry;
    }

    private string FormatAffixGroup(IReadOnlyList<AffixDefinition> affixes)
    {
        var lines = AffixFormatter.FormatEquipmentLinesCn(affixes, ContentRepository);
        if (lines.Count != 1)
        {
            throw new InvalidOperationException($"Expected a single equipment affix group, but got {lines.Count} lines.");
        }

        return lines[0];
    }

    private static async ValueTask<int> ChooseAsync(
        IRuntimeHost host,
        string speaker,
        string prompt,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken)
    {
        var index = await host.ChooseOptionAsync(
            new ChoiceContext(
                speaker,
                prompt,
                options.Select((option, optionIndex) => new ChoiceOptionView(optionIndex, option)).ToArray()),
            cancellationToken);
        if (index < 0 || index >= options.Count)
        {
            throw new InvalidOperationException(
                $"Choice selection index {index} is out of range for {options.Count} options.");
        }

        return index;
    }

    private static bool IsRefinableSlot(EquipmentSlotType slotType) =>
        slotType is EquipmentSlotType.Weapon or EquipmentSlotType.Armor or EquipmentSlotType.Accessory;
}
