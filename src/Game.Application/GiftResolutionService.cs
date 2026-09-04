using Game.Core.Definitions;

namespace Game.Application;

/// <summary>
/// Legacy gift-item selection (赠送物品/赠送物品2): the player picks one
/// backpack item and follow-up branches read the 1-based index of the pick
/// within the instruction list through the <c>wpxz</c> story variable
/// (0 means a wrong item or no pick). Both legacy forms merge here.
/// </summary>
public static class GiftResolutionService
{
    public const string GiftVariableName = "wpxz";

    public static int ResolveGiftIndex(ItemDefinition? picked, IReadOnlyList<string> candidates)
    {
        if (picked is null || candidates.Count == 0)
        {
            return 0;
        }

        for (var index = 0; index < candidates.Count; index++)
        {
            if (string.Equals(picked.Id, candidates[index], StringComparison.Ordinal) ||
                string.Equals(picked.Name, candidates[index], StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return 0;
    }
}
