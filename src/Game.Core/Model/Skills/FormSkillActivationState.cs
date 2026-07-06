using Game.Core.Definitions.Skills;

namespace Game.Core.Model.Skills;

internal sealed class FormSkillActivationState
{
    private readonly IReadOnlyList<FormSkillDefinition> _definitions;
    private readonly HashSet<string> _disabledFormSkillIds;

    public FormSkillActivationState(
        IReadOnlyList<FormSkillDefinition> definitions,
        IEnumerable<string>? disabledFormSkillIds)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        _definitions = definitions;
        _disabledFormSkillIds = new HashSet<string>(disabledFormSkillIds ?? [], StringComparer.Ordinal);
    }

    public IReadOnlySet<string> DisabledFormSkillIds => _disabledFormSkillIds;

    public bool IsEnabled(string sourceSkillId, string formSkillId)
    {
        EnsureFormSkillExists(sourceSkillId, formSkillId);
        return !_disabledFormSkillIds.Contains(formSkillId);
    }

    public bool SetActive(string sourceSkillId, string formSkillId, bool isActive)
    {
        EnsureFormSkillExists(sourceSkillId, formSkillId);
        return isActive
            ? _disabledFormSkillIds.Remove(formSkillId)
            : _disabledFormSkillIds.Add(formSkillId);
    }

    private void EnsureFormSkillExists(string sourceSkillId, string formSkillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formSkillId);
        if (!_definitions.Any(formSkill => string.Equals(formSkill.Id, formSkillId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Skill '{sourceSkillId}' has no form skill '{formSkillId}'.");
        }
    }
}
