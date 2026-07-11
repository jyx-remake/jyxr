using Game.Core.Battle;
using Godot;

namespace Game.Godot.UI.Battle;

internal static class BattleFloatTextTheme
{
    public static Color ResolveColor(BattleFloatTextStyle style) => style switch
    {
        BattleFloatTextStyle.Critical => new Color("ffd84d"),
        BattleFloatTextStyle.Recovery => new Color("55e06f"),
        BattleFloatTextStyle.Mana => new Color("55a7ff"),
        BattleFloatTextStyle.Energy => new Color("45e0e6"),
        BattleFloatTextStyle.Beneficial => new Color("8be28b"),
        BattleFloatTextStyle.Harmful => new Color("ff6262"),
        BattleFloatTextStyle.Special => new Color("f06cff"),
        _ => Colors.White,
    };
}
