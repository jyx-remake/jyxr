using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using static Game.Core.Model.Character.CharacterInstance;

namespace Game.Application;

public sealed class InventoryService
{
    private readonly GameSession _session;

    public InventoryService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

    private Inventory Inventory => State.Inventory;

    private EquipmentInstanceFactory EquipmentInstanceFactory => State.EquipmentInstanceFactory;

    public StackInventoryEntry AddItem(string itemId, int quantity = 1, bool notifyAcquisition = true) =>
        AddItem(_session.ContentRepository.GetItem(itemId), quantity, notifyAcquisition);

    public StackInventoryEntry AddItem(ItemDefinition item, int quantity = 1, bool notifyAcquisition = true)
    {
        ArgumentNullException.ThrowIfNull(item);

        var entry = Inventory.AddItem(item, quantity);
        _session.Events.Publish(new InventoryChangedEvent());
        if (notifyAcquisition)
        {
            _session.Events.Publish(new ItemAcquiredEvent(item.Id, item.Name, quantity));
        }
        return entry;
    }

    public InventoryEntry AddEquipmentInstance(
        EquipmentDefinition equipment,
        IReadOnlyList<AffixDefinition>? extraAffixes = null)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        var instance = EquipmentInstanceFactory.Create(equipment, extraAffixes);
        var entry = Inventory.AddEquipmentInstance(instance);
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new ItemAcquiredEvent(equipment.Id, equipment.Name, 1));
        return entry;
    }

    public void RemoveItem(string itemId, int quantity = 1) =>
        RemoveItem(_session.ContentRepository.GetItem(itemId), quantity);

    public void RemoveItem(ItemDefinition item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);

        Inventory.RemoveItem(item, quantity);
        _session.Events.Publish(new InventoryChangedEvent());
    }

    public void EquipFromStack(string characterId, EquipmentDefinition equipmentDefinition) =>
        EquipFromStack(State.Party.GetMember(characterId), equipmentDefinition);

    public void EquipFromStack(CharacterInstance character, EquipmentDefinition equipmentDefinition)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(equipmentDefinition);

        Inventory.RemoveItem(equipmentDefinition);
        ReplaceEquippedItem(character, equipmentDefinition.SlotType);
        var created = EquipmentInstanceFactory.Create(equipmentDefinition);
        character.AddEquipmentInstance(created);
        GrantEquipmentSkills(character, created);
        character.RebuildSnapshot();
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void EquipInstance(string characterId, string equipmentInstanceId) =>
        EquipInstance(State.Party.GetMember(characterId), equipmentInstanceId);

    public void EquipInstance(CharacterInstance character, string equipmentInstanceId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentInstanceId);

        var entry = Inventory.GetEquipmentInstanceEntry(equipmentInstanceId);

        var equipment = Inventory.RemoveEquipmentInstance(equipmentInstanceId);
        ReplaceEquippedItem(character, equipment.Definition.SlotType);
        character.AddEquipmentInstance(equipment);
        GrantEquipmentSkills(character, equipment);
        character.RebuildSnapshot();
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public EquipmentInstance UnequipToInventory(string characterId, EquipmentSlotType slotType) =>
        UnequipToInventory(State.Party.GetMember(characterId), slotType);

    public EquipmentInstance UnequipToInventory(CharacterInstance character, EquipmentSlotType slotType)
    {
        ArgumentNullException.ThrowIfNull(character);

        var equipment = character.RemoveEquipment(slotType);
        RevokeEquipmentSkills(character, equipment);
        character.RebuildSnapshot();
        Inventory.AddEquipmentInstance(equipment);
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
        return equipment;
    }

    public void UnequipAllToInventory(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.EquippedItems.Count == 0)
        {
            return;
        }

        foreach (var slotType in character.EquippedItems.Keys.ToArray())
        {
            var removed = character.RemoveEquipment(slotType);
            RevokeEquipmentSkills(character, removed);
            Inventory.AddEquipmentInstance(removed);
        }

        character.RebuildSnapshot();
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    private void ReplaceEquippedItem(CharacterInstance character, EquipmentSlotType slotType)
    {
        var equipped = character.GetEquipment(slotType);
        if (equipped is null)
        {
            return;
        }

        var removed = character.RemoveEquipment(slotType);
        RevokeEquipmentSkills(character, removed);
        Inventory.AddEquipmentInstance(removed);
    }

    /// <summary>
    /// Rebuilds equipment-granted skill provenance from currently equipped
    /// items. Runs after load and new-game setup, when fresh character
    /// instances have no provenance yet. Idempotent: already-owned skills
    /// are only recorded, never duplicated.
    /// </summary>
    public void RestoreEquipmentGrantedSkills()
    {
        foreach (var character in State.Party.GetAllCharacters())
        {
            var changed = false;
            foreach (var equipment in character.EquippedItems.Values)
            {
                changed |= GrantEquipmentSkills(character, equipment);
            }

            if (changed)
            {
                character.RebuildSnapshot();
            }
        }
    }

    /// <summary>
    /// Learns the equipment's carried skills/specials. Returns whether
    /// anything was newly learned. Provenance records whether this grant
    /// created the instance: pre-owned skills are only covered, never
    /// stolen on unequip.
    /// </summary>
    private bool GrantEquipmentSkills(CharacterInstance character, EquipmentInstance equipment)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(equipment);

        var granted = character.EquipmentGrantedSkills.GetValueOrDefault(equipment.Id);
        granted ??= [];
        character.EquipmentGrantedSkills[equipment.Id] = granted;

        var changed = false;
        foreach (var grantedSkill in equipment.Definition.GrantedSkills)
        {
            var owned = character.ExternalSkills.Any(skill => string.Equals(skill.Id, grantedSkill.SkillId, StringComparison.Ordinal));
            if (!granted.Any(key => key.Kind == SkillKind.External && string.Equals(key.SkillId, grantedSkill.SkillId, StringComparison.Ordinal)))
            {
                granted.Add(new EquipmentGrantedSkillKey(SkillKind.External, grantedSkill.SkillId, !owned));
            }

            if (owned)
            {
                continue;
            }

            var definition = _session.ContentRepository.GetExternalSkill(grantedSkill.SkillId);
            character.SetExternalSkillState(definition, Math.Max(1, grantedSkill.Level), 0, true);
            changed = true;
        }

        foreach (var grantedSpecial in equipment.Definition.GrantedSpecialSkills)
        {
            var owned = character.SpecialSkills.Any(skill => string.Equals(skill.Definition.Id, grantedSpecial.SkillId, StringComparison.Ordinal));
            if (!granted.Any(key => key.Kind == SkillKind.Special && string.Equals(key.SkillId, grantedSpecial.SkillId, StringComparison.Ordinal)))
            {
                granted.Add(new EquipmentGrantedSkillKey(SkillKind.Special, grantedSpecial.SkillId, !owned));
            }

            if (owned)
            {
                continue;
            }

            character.LearnSpecialSkill(_session.ContentRepository.GetSpecialSkill(grantedSpecial.SkillId));
            changed = true;
        }

        return changed;
    }

    private void RevokeEquipmentSkills(CharacterInstance character, EquipmentInstance equipment)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(equipment);

        if (!character.EquipmentGrantedSkills.Remove(equipment.Id, out var granted) || granted.Count == 0)
        {
            return;
        }

        var removed = false;
        foreach (var key in granted)
        {
            if (!key.CreatedByGrant || PromoteCoveringEntry(character, key))
            {
                continue;
            }

            if (key.Kind == SkillKind.External)
            {
                removed |= character.RemoveExternalSkill(key.SkillId);
            }
            else if (key.Kind == SkillKind.Special)
            {
                removed |= character.RemoveSpecialSkill(key.SkillId);
            }
        }

        if (removed)
        {
            character.RebuildSnapshot();
        }
    }

    /// <summary>
    /// Hands instance ownership to another still-equipped gear covering the
    /// same skill, so the instance outlives the gear that created it exactly
    /// while some gear still covers it.
    /// </summary>
    private static bool PromoteCoveringEntry(CharacterInstance character, EquipmentGrantedSkillKey key)
    {
        foreach (var other in character.EquipmentGrantedSkills)
        {
            for (var index = 0; index < other.Value.Count; index++)
            {
                var otherKey = other.Value[index];
                if (otherKey.Kind == key.Kind &&
                    string.Equals(otherKey.SkillId, key.SkillId, StringComparison.Ordinal))
                {
                    other.Value[index] = otherKey with { CreatedByGrant = true };
                    return true;
                }
            }
        }

        return false;
    }
}
