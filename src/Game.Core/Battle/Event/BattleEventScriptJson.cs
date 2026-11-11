using Game.Core.Battle;
using Game.Core.Serialization;
using System.Text.Json.Nodes;

namespace Game.Core.Battle;

public static class BattleEventScriptJson
{
    public static BattleEventScript LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream, path);
    }

    public static BattleEventScript Load(Stream stream, string sourceName = "battleevent")
    {
        var root = GameJson.ParseNode(stream);
        return new BattleEventScriptJsonParser(root, sourceName).Parse();
    }

    public static BattleEventScript Parse(string json, string sourceName = "battleevent")
    {
        var root = GameJson.ParseNode(json);
        return new BattleEventScriptJsonParser(root, sourceName).Parse();
    }

    public static BattleEventScript Parse(JsonObject root, string sourceName = "battleevent") =>
        new BattleEventScriptJsonParser(root, sourceName).Parse();
}
