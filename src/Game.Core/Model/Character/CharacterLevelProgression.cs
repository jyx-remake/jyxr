namespace Game.Core.Model.Character;

public static class CharacterLevelProgression
{
    public const int MaxLevel = 30;

    public static int GetLevelUpExperience(int level)
    {
        if (level <= 0)
        {
            return 0;
        }

        var required = 0;
        for (var currentLevel = 1; currentLevel <= level; currentLevel += 1)
        {
            required = checked((int)(currentLevel * 20 + 1.1 * required));
        }

        return required;
    }

    public static int GetTotalExperienceRequiredForLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);

        var totalRequired = 0;
        for (var currentLevel = 1; currentLevel < level; currentLevel += 1)
        {
            totalRequired = checked(totalRequired + GetLevelUpExperience(currentLevel));
        }

        return totalRequired;
    }

    public static int ResolveLevel(int totalExperience)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalExperience);

        var resolvedLevel = 1;
        var requiredTotalExperience = 0;
        for (var currentLevel = 1; currentLevel < MaxLevel; currentLevel += 1)
        {
            requiredTotalExperience = checked(requiredTotalExperience + GetLevelUpExperience(currentLevel));
            if (totalExperience < requiredTotalExperience)
            {
                break;
            }

            resolvedLevel = currentLevel + 1;
        }

        return resolvedLevel;
    }

    public static (int CurrentExperience, int NextLevelExperience) GetDisplayProgress(int level, int totalExperience)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalExperience);

        var clampedLevel = Math.Min(level, MaxLevel);
        var nextLevelExperience = GetLevelUpExperience(clampedLevel);
        if (clampedLevel >= MaxLevel)
        {
            return (nextLevelExperience, nextLevelExperience);
        }

        var currentLevelStartExperience = GetTotalExperienceRequiredForLevel(clampedLevel);
        var currentExperience = Math.Clamp(totalExperience - currentLevelStartExperience, 0, nextLevelExperience);
        return (currentExperience, nextLevelExperience);
    }
}
