using Game.Application.Formatters;
using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class EquipmentRefinementService
{
    private const int CandidateCount = 8;
    private const string NoEquipmentStoryId = "洗练_没有装备";
    private const string CancelStoryId = "洗练选择";
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

        var equipmentEntries = State.Inventory.Entries
            .OfType<EquipmentInstanceInventoryEntry>()
            .Where(static entry => IsRefinableSlot(entry.Equipment.Definition.SlotType))
            .Where(static entry => entry.Equipment.ExtraAffixes.Count > 0)
            .ToArray();
        if (equipmentEntries.Length == 0)
        {
            return StoryCommandResult.Jump(NoEquipmentStoryId);
        }

        var selectedEntry = await SelectEquipmentAsync(host, equipmentEntries, cancellationToken);
        if (selectedEntry is null)
        {
            return StoryCommandResult.Jump(CancelStoryId);
        }

        return await RunRefinementLoopAsync(host, selectedEntry.Equipment, cancellationToken);
    }

    private async ValueTask<StoryCommandResult> RunRefinementLoopAsync(
        IRuntimeHost host,
        EquipmentInstance equipment,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var affixGroups = EquipmentAffixGroups.Group(equipment.ExtraAffixes);
            var affixTexts = affixGroups
                .Select(group => FormatAffixGroup(group.Affixes))
                .ToArray();
            var oldAffixOptions = affixTexts
                .Append(ExitOptionText)
                .ToArray();
            var selectedAffixIndex = await ChooseAsync(
                host,
                "主角",
                "选择要替换的旧词条",
                oldAffixOptions,
                cancellationToken);
            if (selectedAffixIndex == affixGroups.Count)
            {
                return StoryCommandResult.Jump(CancelStoryId);
            }

            if (!_session.ProfileService.CanSpendYuanbao(1))
            {
                return StoryCommandResult.Jump(InsufficientYuanbaoStoryId);
            }

            var candidates = GenerateCandidates(equipment, affixTexts);
            _session.ProfileService.SpendYuanbao(1);

            var candidateTexts = candidates
                .Select(candidate => FormatAffixGroup(candidate.Affixes))
                .Append(CancelOptionText)
                .ToArray();
            var candidateIndex = await ChooseAsync(
                host,
                "主角",
                "选择新的附加词条",
                candidateTexts,
                cancellationToken);
            if (candidateIndex == candidates.Count)
            {
                continue;
            }

            ReplaceAffixGroup(equipment, affixGroups[selectedAffixIndex], candidates[candidateIndex].Affixes);
            _session.Events.Publish(new InventoryChangedEvent());
            _session.Events.Publish(new ToastRequestedEvent("洗练成功！"));
            await host.ExecuteCommandAsync(
                "effect",
                [ExprValue.FromString(SuccessEffectId)],
                cancellationToken);
        }
    }

    private IReadOnlyList<GeneratedEquipmentAffixRoll> GenerateCandidates(
        EquipmentInstance equipment,
        IReadOnlyCollection<string> currentAffixTexts)
    {
        var candidates = new List<GeneratedEquipmentAffixRoll>(CandidateCount);
        for (var candidateIndex = 0; candidateIndex < CandidateCount; candidateIndex++)
        {
            candidates.Add(GenerateCandidate(equipment, currentAffixTexts));
        }

        return candidates;
    }

    private GeneratedEquipmentAffixRoll GenerateCandidate(
        EquipmentInstance equipment,
        IReadOnlyCollection<string> currentAffixTexts)
    {
        for (var attempt = 0; attempt < 4096; attempt++)
        {
            var roll = EquipmentRandomAffixGenerator.GenerateSingleRoll(
                equipment.Definition,
                ContentRepository,
                State.Adventure.Round);
            if (!currentAffixTexts.Contains(FormatAffixGroup(roll.Affixes), StringComparer.Ordinal))
            {
                return roll;
            }
        }

        throw new InvalidOperationException(
            $"Equipment '{equipment.Definition.Id}' cannot generate a refinement candidate outside its current affixes.");
    }

    private void ReplaceAffixGroup(
        EquipmentInstance equipment,
        EquipmentAffixGroup oldGroup,
        IReadOnlyList<AffixDefinition> newAffixes)
    {
        var affixes = equipment.ExtraAffixes.ToList();
        affixes.RemoveRange(oldGroup.StartIndex, oldGroup.Count);
        affixes.InsertRange(oldGroup.StartIndex, newAffixes);
        equipment.ReplaceExtraAffixes(affixes);
    }

    private async ValueTask<EquipmentInstanceInventoryEntry?> SelectEquipmentAsync(
        IRuntimeHost host,
        IReadOnlyList<EquipmentInstanceInventoryEntry> equipmentEntries,
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
