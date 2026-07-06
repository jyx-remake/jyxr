namespace Game.Core.Model.Skills;

public interface IFormSkillSource
{
    IReadOnlySet<string> DisabledFormSkillIds { get; }

    bool IsFormSkillEnabled(string formSkillId);

    bool SetFormSkillActive(string formSkillId, bool isActive);
}
