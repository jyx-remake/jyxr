using Game.Core.Model;

namespace Game.Application;

/// <summary>
/// Legacy XMJH addressed the protagonist through placeholder party members
/// (性别1/性别2/性别3) that were joined and renamed per gender branch
/// (少侠/师弟/公子 for a male hero, 女侠/师妹/小姐 for a female hero).
/// The new model resolves those aliases directly from the protagonist's
/// gender; non-female genders (male/neutral/eunuch/animal) use the male form.
/// </summary>
internal static class StoryGenderAlias
{
    public static bool TryResolve(string variableName, CharacterGender heroGender, out string value)
    {
        var (maleForm, femaleForm, known) = variableName switch
        {
            "性别1" => ("少侠", "女侠", true),
            "性别2" => ("师弟", "师妹", true),
            "性别3" => ("公子", "小姐", true),
            _ => (string.Empty, string.Empty, false),
        };

        if (!known)
        {
            value = string.Empty;
            return false;
        }

        value = heroGender == CharacterGender.Female ? femaleForm : maleForm;
        return true;
    }
}
