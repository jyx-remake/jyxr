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
    private const string SuccessStoryId = "洗练_洗练成功";
    private const string CancelOptionText = "不替换了";
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

        var equipmentIndex = await ChooseAsync(
            host,
            "主角",
            "选择要洗练的装备",
            equipmentEntries.Select(FormatEquipmentOption).ToArray(),
            cancellationToken);
        var equipment = equipmentEntries[equipmentIndex].Equipment;

        var affixGroups = EquipmentAffixGroups.Group(equipment.ExtraAffixes);
        var affixTexts = affixGroups
            .Select(group => FormatAffixGroup(group.Affixes))
            .ToArray();
        var selectedAffixIndex = await ChooseAsync(
            host,
            "主角",
            "选择要替换的旧词条",
            affixTexts,
            cancellationToken);
        var affixIndex = Array.FindIndex(
            affixTexts,
            text => string.Equals(text, affixTexts[selectedAffixIndex], StringComparison.Ordinal));

        var candidates = GenerateCandidates(equipment, affixTexts);
        State.Currency.SpendGold(1);
        _session.Events.Publish(new CurrencyChangedEvent());

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
            return StoryCommandResult.Jump(CancelStoryId);
        }

        ReplaceAffixGroup(equipment, affixGroups[affixIndex], candidates[candidateIndex].Affixes);
        _session.Events.Publish(new InventoryChangedEvent());
        await host.ExecuteCommandAsync(
            "effect",
            [ExprValue.FromString(SuccessEffectId)],
            cancellationToken);
        return StoryCommandResult.Jump(SuccessStoryId);
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

    private string FormatEquipmentOption(EquipmentInstanceInventoryEntry entry)
    {
        var lines = AffixFormatter.FormatEquipmentLinesCn(entry.Equipment.ExtraAffixes, ContentRepository);
        return $"{entry.Equipment.Definition.Name}：{string.Join("；", lines)}";
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
