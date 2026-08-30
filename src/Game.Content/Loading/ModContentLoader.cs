using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed record LoadedModContent(
    GameConfig Config,
    InMemoryContentRepository Repository,
    ContentLoadReport Report);

public sealed record ContentLoadWarning(
    string Target,
    string PreviousModId,
    string CurrentModId,
    string Message);

public sealed record ContentLoadReport(IReadOnlyList<ContentLoadWarning> Warnings);

internal static class ModContentLoader
{
    private const string GameConfigFileName = "game-config.json";
    private const string PatchFilePattern = "*.patch.json";
    private const string StoryDirectoryName = "stories";
    private const string StoryFilePattern = "*.story.json";

    public static LoadedModContent Load(IReadOnlyList<ModContentInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        JsonContentLoader.Ensure(inputs.Count > 0, "At least one mod is required.");
        JsonContentLoader.Ensure(inputs[0].Required, "The first mod must be the required primary package.");

        var catalog = new RawContentCatalog();
        foreach (var input in inputs)
        {
            ValidateInput(input);
            try
            {
                LoadMod(catalog, input);
            }
            catch (Exception exception) when (exception is not ContentLoadException)
            {
                throw new ContentLoadException(
                    $"Failed to load content from mod '{input.ModId}' at '{input.ModDirectoryPath}': {exception.Message}",
                    exception);
            }
        }

        var config = DeserializeRequired<GameConfig>(catalog.GameConfig, "gameConfig");
        ValidateConfig(config);
        var package = BuildPackage(catalog);
        var repository = new JsonContentLoader().LoadFromPackage(package);
        JsonContentLoader.ValidateGameConfigMediaReferences(config, repository);
        return new LoadedModContent(config, repository, new ContentLoadReport(catalog.Warnings.ToArray()));
    }

    private static void ValidateInput(ModContentInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ModId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ModDirectoryPath);
        if (!Directory.Exists(input.ModDirectoryPath))
        {
            if (input.Required)
            {
                throw new DirectoryNotFoundException($"Mod directory '{input.ModDirectoryPath}' was not found.");
            }

            return;
        }

        if (input.Required && !Directory.Exists(input.DataDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Content directory '{input.DataDirectoryPath}' was not found.");
        }
    }

    private static void LoadMod(RawContentCatalog catalog, ModContentInput input)
    {
        foreach (var spec in ContentTypeCatalog.All)
        {
            var sourceRequired = input.Required && !string.Equals(spec.Kind, "characterTitle", StringComparison.Ordinal);
            foreach (var entry in DefinitionSourceReader.Read(input.DataDirectoryPath, spec, sourceRequired))
            {
                var id = GetRequiredString(entry.Definition, "id", entry.FilePath);
                catalog.AddDefinition(
                    spec.Kind,
                    id,
                    entry.Definition,
                    input.ModId,
                    entry.FilePath);
            }
        }

        LoadStoryFiles(catalog, input);
        LoadGameConfig(catalog, input);
        LoadPatchFiles(catalog, input);
    }

    private static void LoadStoryFiles(RawContentCatalog catalog, ModContentInput input)
    {
        var storyDirectory = Path.Combine(input.DataDirectoryPath, StoryDirectoryName);
        if (!Directory.Exists(storyDirectory))
        {
            return;
        }

        foreach (var storyPath in Directory.GetFiles(storyDirectory, StoryFilePattern, SearchOption.AllDirectories)
                     .OrderBy(static path => path, StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(storyDirectory, storyPath).Replace('\\', '/');
            const string suffix = ".story.json";
            JsonContentLoader.Ensure(relativePath.EndsWith(suffix, StringComparison.Ordinal),
                $"Story file '{relativePath}' must end with '{suffix}'.");
            var scriptId = relativePath[..^suffix.Length];
            var script = ParseNode(storyPath) as JsonObject
                ?? throw new ContentLoadException($"Story file '{storyPath}' must contain a JSON object.");
            catalog.AddStoryScript(scriptId, script, input.ModId, storyPath);
        }
    }

    private static void LoadGameConfig(RawContentCatalog catalog, ModContentInput input)
    {
        var configPath = Path.Combine(input.DataDirectoryPath, GameConfigFileName);
        if (input.Required)
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Primary mod game config was not found: {configPath}", configPath);
            }

            var config = ParseNode(configPath) as JsonObject
                ?? throw new ContentLoadException($"Game config '{configPath}' must contain a JSON object.");
            catalog.SetGameConfig(config, input.ModId, configPath);
            return;
        }

        if (File.Exists(configPath))
        {
            throw new ContentLoadException(
                $"Addon '{input.ModId}' cannot provide a complete '{GameConfigFileName}'. Use a gameConfig patch instead.");
        }
    }

    private static void LoadPatchFiles(RawContentCatalog catalog, ModContentInput input)
    {
        var patchDirectory = input.PatchDirectoryPath;
        if (!Directory.Exists(patchDirectory))
        {
            return;
        }

        foreach (var patchPath in Directory.GetFiles(patchDirectory, PatchFilePattern, SearchOption.AllDirectories)
                     .OrderBy(static path => path, StringComparer.Ordinal))
        {
            var document = ParseNode(patchPath) as JsonObject
                ?? throw new ContentLoadException($"Patch file '{patchPath}' must contain a JSON object.");
            EnsureOnlyProperties(document, patchPath, "format", "operations");
            var format = document["format"]?.GetValue<int>()
                ?? throw new ContentLoadException($"Patch file '{patchPath}' is missing integer 'format'.");
            if (format != 2)
            {
                throw new ContentLoadException($"Patch file '{patchPath}' has unsupported format '{format}'.");
            }
            var operations = document["operations"] as JsonArray
                ?? throw new ContentLoadException($"Patch file '{patchPath}' is missing array 'operations'.");

            for (var index = 0; index < operations.Count; index++)
            {
                var operation = operations[index] as JsonObject
                    ?? throw new ContentLoadException($"Patch operation {index} in '{patchPath}' must be an object.");
                try
                {
                    ApplyOperation(catalog, operation, new PatchSource(input.ModId, patchPath, index));
                }
                catch (Exception exception) when (exception is not ContentLoadException)
                {
                    throw new ContentLoadException(
                        $"Patch operation {index} in '{patchPath}' failed: {exception.Message}", exception);
                }
            }
        }
    }

    private static void ApplyOperation(RawContentCatalog catalog, JsonObject operation, PatchSource source)
    {
        var op = GetRequiredString(operation, "op", source.Description);
        var targetObject = operation["target"] as JsonObject
            ?? throw new ContentLoadException($"{source.Description} is missing object 'target'.");
        EnsureOnlyProperties(targetObject, source.Description, "kind", "id");
        var kind = GetRequiredString(targetObject, "kind", source.Description);
        var id = targetObject["id"]?.GetValue<string>();
        var target = catalog.GetTarget(kind, id, source);
        var path = ParsePath(operation, source);

        switch (op)
        {
            case "merge":
            {
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "value");
                var value = operation["value"] as JsonObject
                    ?? throw new ContentLoadException($"{source.Description} merge operation requires object 'value'.");
                var resolved = ResolveNode(target, path, source);
                var destination = resolved.Node as JsonObject
                    ?? throw new ContentLoadException(
                        $"{source.Description} merge target '{target.Address}{resolved.Address}' must be an object.");
                EnsureMergeIdentityUnchanged(target, path, resolved, value, source);
                ApplyMerge(catalog, target, destination, value, resolved.Address, source);
                break;
            }
            case "set":
            {
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "value");
                if (!operation.TryGetPropertyValue("value", out var rawValue))
                {
                    throw new ContentLoadException($"{source.Description} set operation requires 'value'.");
                }

                var value = rawValue?.DeepClone();
                if (path.Count == 0)
                {
                    var replacement = value as JsonObject
                        ?? throw new ContentLoadException($"{source.Description} root set value must be an object.");
                    EnsureReplacementIdentity(target, replacement, source);
                    catalog.ReplaceTarget(target, replacement, source);
                }
                else
                {
                    SetAtPath(catalog, target, path, value, source);
                }

                break;
            }
            case "remove":
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path");
                if (path.Count == 0)
                {
                    catalog.RemoveTarget(target, source);
                }
                else
                {
                    RemoveAtPath(catalog, target, path, source);
                }

                break;
            case "test":
            {
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "value");
                if (!operation.TryGetPropertyValue("value", out var expected))
                {
                    throw new ContentLoadException($"{source.Description} test operation requires 'value'.");
                }

                var resolved = ResolveNode(target, path, source);
                if (!JsonNode.DeepEquals(resolved.Node, expected))
                {
                    throw new ContentLoadException(
                        $"{source.Description} test failed at '{target.Address}{resolved.Address}'. " +
                        $"Expected {Format(expected)}, actual {Format(resolved.Node)}.");
                }

                break;
            }
            case "append":
            case "prepend":
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "values");
                ApplyAppend(catalog, target, path, operation, op, source);
                break;
            case "insertBefore":
            case "insertAfter":
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "anchor", "value");
                ApplyInsert(catalog, target, path, operation, op, source);
                break;
            case "moveBefore":
            case "moveAfter":
                EnsureOnlyProperties(operation, source.Description, "op", "target", "path", "item", "anchor");
                ApplyMove(catalog, target, path, operation, op, source);
                break;
            default:
                throw new ContentLoadException($"{source.Description} uses unsupported operation '{op}'.");
        }
    }

    private static IReadOnlyList<PatchPathSegment> ParsePath(JsonObject operation, PatchSource source)
    {
        if (!operation.TryGetPropertyValue("path", out var pathNode))
        {
            return [];
        }

        var path = pathNode as JsonArray
            ?? throw new ContentLoadException($"{source.Description} field 'path' must be an array.");
        var segments = new List<PatchPathSegment>(path.Count);
        for (var index = 0; index < path.Count; index++)
        {
            var segment = path[index];
            if (segment is JsonValue propertyValue &&
                propertyValue.TryGetValue<string>(out var propertyName) &&
                !string.IsNullOrWhiteSpace(propertyName))
            {
                segments.Add(PatchPathSegment.ForProperty(propertyName));
                continue;
            }

            if (segment is JsonObject selector)
            {
                EnsureOnlyProperties(selector, $"{source.Description} path segment {index}", "id");
                segments.Add(PatchPathSegment.ForId(GetRequiredString(selector, "id", source.Description)));
                continue;
            }

            throw new ContentLoadException(
                $"{source.Description} path segment {index} must be a non-empty property name or an object containing 'id'.");
        }

        return segments;
    }

    private static void ApplyMerge(
        RawContentCatalog catalog,
        RawTarget target,
        JsonObject destination,
        JsonObject patch,
        string path,
        PatchSource source)
    {
        foreach (var (propertyName, patchValue) in patch)
        {
            var propertyPath = path + "/" + EscapePointer(propertyName);
            if (patchValue is JsonObject childPatch && destination[propertyName] is JsonObject childDestination)
            {
                ApplyMerge(catalog, target, childDestination, childPatch, propertyPath, source);
                continue;
            }

            var replacement = patchValue?.DeepClone();
            catalog.RecordWrite(target, propertyPath, destination[propertyName], replacement, source);
            destination[propertyName] = replacement;
        }
    }

    private static void ApplyAppend(
        RawContentCatalog catalog,
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        JsonObject operation,
        string op,
        PatchSource source)
    {
        var resolved = ResolveNode(target, path, source);
        var list = resolved.Node as JsonArray
            ?? throw new ContentLoadException(
                $"{source.Description} {op} target '{target.Address}{resolved.Address}' must be an array.");
        var values = operation["values"] as JsonArray
            ?? throw new ContentLoadException($"{source.Description} {op} operation requires array 'values'.");
        var clones = values.Select(static value => value?.DeepClone()).ToArray();
        if (op == "append")
        {
            foreach (var value in clones)
            {
                list.Add(value);
            }
        }
        else
        {
            for (var index = clones.Length - 1; index >= 0; index--)
            {
                list.Insert(0, clones[index]);
            }
        }

        ValidateListIds(list, target, resolved.Address, source);
        catalog.RecordAppend(target, resolved.Address, source);
    }

    private static void ApplyInsert(
        RawContentCatalog catalog,
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        JsonObject operation,
        string op,
        PatchSource source)
    {
        var resolved = ResolveNode(target, path, source);
        var list = resolved.Node as JsonArray
            ?? throw new ContentLoadException(
                $"{source.Description} {op} target '{target.Address}{resolved.Address}' must be an array.");
        var anchorId = GetSelectorId(operation, "anchor", source);
        var anchor = FindListItem(list, anchorId, target, resolved.Address, source);
        var value = operation["value"] as JsonObject
            ?? throw new ContentLoadException($"{source.Description} {op} operation requires object 'value'.");
        var insertedId = GetRequiredString(value, "id", source.Description);
        EnsureListIdMissing(list, insertedId, target, resolved.Address, source);
        list.Insert(op == "insertBefore" ? anchor.Index : anchor.Index + 1, value.DeepClone());
        ValidateListIds(list, target, resolved.Address, source);
        catalog.RecordAppend(target, resolved.Address, source);
    }

    private static void ApplyMove(
        RawContentCatalog catalog,
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        JsonObject operation,
        string op,
        PatchSource source)
    {
        var resolved = ResolveNode(target, path, source);
        var list = resolved.Node as JsonArray
            ?? throw new ContentLoadException(
                $"{source.Description} {op} target '{target.Address}{resolved.Address}' must be an array.");
        var itemId = GetSelectorId(operation, "item", source);
        var anchorId = GetSelectorId(operation, "anchor", source);
        if (string.Equals(itemId, anchorId, StringComparison.Ordinal))
        {
            throw new ContentLoadException($"{source.Description} cannot move list item '{itemId}' relative to itself.");
        }

        var previous = list.DeepClone();
        var item = FindListItem(list, itemId, target, resolved.Address, source);
        var moved = item.Node;
        list.RemoveAt(item.Index);
        var anchor = FindListItem(list, anchorId, target, resolved.Address, source);
        list.Insert(op == "moveBefore" ? anchor.Index : anchor.Index + 1, moved);
        catalog.RecordWrite(target, resolved.Address, previous, list, source);
    }

    private static void SetAtPath(
        RawContentCatalog catalog,
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        JsonNode? value,
        PatchSource source)
    {
        var parent = ResolveNode(target, path.Take(path.Count - 1).ToArray(), source);
        var last = path[^1];
        if (last.PropertyName is not null)
        {
            var destination = parent.Node as JsonObject
                ?? throw new ContentLoadException(
                    $"{source.Description} set parent '{target.Address}{parent.Address}' must be an object.");
            EnsurePropertyIsNotIdentity(target, path, parent, last.PropertyName, source);
            destination.TryGetPropertyValue(last.PropertyName, out var previous);
            var propertyAddress = parent.Address + "/" + EscapePointer(last.PropertyName);
            catalog.RecordWrite(target, propertyAddress, previous, value, source);
            destination[last.PropertyName] = value;
            return;
        }

        var list = parent.Node as JsonArray
            ?? throw new ContentLoadException(
                $"{source.Description} set selector parent '{target.Address}{parent.Address}' must be an array.");
        var selected = FindListItem(list, last.Id!, target, parent.Address, source);
        var replacement = value as JsonObject
            ?? throw new ContentLoadException($"{source.Description} set value for list item '{last.Id}' must be an object.");
        var replacementId = GetRequiredString(replacement, "id", source.Description);
        JsonContentLoader.Ensure(string.Equals(replacementId, last.Id, StringComparison.Ordinal),
            $"{source.Description} set value must preserve list item id '{last.Id}'.");
        var listItemAddress = parent.Address + "/@" + EscapePointer(last.Id!);
        catalog.RecordWrite(target, listItemAddress, selected.Node, replacement, source);
        list[selected.Index] = replacement.DeepClone();
    }

    private static void RemoveAtPath(
        RawContentCatalog catalog,
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        PatchSource source)
    {
        var parent = ResolveNode(target, path.Take(path.Count - 1).ToArray(), source);
        var last = path[^1];
        if (last.PropertyName is not null)
        {
            var destination = parent.Node as JsonObject
                ?? throw new ContentLoadException(
                    $"{source.Description} remove parent '{target.Address}{parent.Address}' must be an object.");
            EnsurePropertyIsNotIdentity(target, path, parent, last.PropertyName, source);
            if (!destination.TryGetPropertyValue(last.PropertyName, out var previous) ||
                !destination.Remove(last.PropertyName))
            {
                throw new ContentLoadException(
                    $"{source.Description} remove target '{target.Address}{parent.Address}/{EscapePointer(last.PropertyName)}' does not exist.");
            }

            var propertyAddress = parent.Address + "/" + EscapePointer(last.PropertyName);
            catalog.RecordWrite(target, propertyAddress, previous, null, source);
            return;
        }

        var list = parent.Node as JsonArray
            ?? throw new ContentLoadException(
                $"{source.Description} remove selector parent '{target.Address}{parent.Address}' must be an array.");
        var selected = FindListItem(list, last.Id!, target, parent.Address, source);
        var listItemAddress = parent.Address + "/@" + EscapePointer(last.Id!);
        catalog.RecordWrite(target, listItemAddress, selected.Node, null, source);
        list.RemoveAt(selected.Index);
    }

    private static ResolvedPath ResolveNode(
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        PatchSource source)
    {
        JsonNode? current = target.Node;
        var address = string.Empty;
        string? selectedId = null;
        foreach (var segment in path)
        {
            if (segment.PropertyName is not null)
            {
                if (current is not JsonObject currentObject ||
                    !currentObject.TryGetPropertyValue(segment.PropertyName, out current))
                {
                    throw new ContentLoadException(
                        $"{source.Description} path '{target.Address}{address}/{EscapePointer(segment.PropertyName)}' does not exist or is not an object field.");
                }

                address += "/" + EscapePointer(segment.PropertyName);
                selectedId = null;
                continue;
            }

            if (current is not JsonArray list)
            {
                throw new ContentLoadException(
                    $"{source.Description} path selector '{segment.Id}' at '{target.Address}{address}' requires an array.");
            }

            var selected = FindListItem(list, segment.Id!, target, address, source);
            current = selected.Node;
            address += "/@" + EscapePointer(segment.Id!);
            selectedId = segment.Id;
        }

        return new ResolvedPath(current, address, selectedId);
    }

    private static ListItem FindListItem(
        JsonArray list,
        string id,
        RawTarget target,
        string path,
        PatchSource source)
    {
        var matches = list
            .Select((node, index) => new ListItem(node as JsonObject, index))
            .Where(item => item.Node is not null &&
                           string.Equals(item.Node["id"]?.GetValue<string>(), id, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new ContentLoadException(
                $"{source.Description} expected one 'id={id}' list item at '{target.Address}{path}', found {matches.Length}.");
        }

        return matches[0] with { Node = matches[0].Node! };
    }

    private static void EnsureListIdMissing(
        JsonArray list,
        string id,
        RawTarget target,
        string path,
        PatchSource source)
    {
        if (list.OfType<JsonObject>().Any(item =>
                string.Equals(item["id"]?.GetValue<string>(), id, StringComparison.Ordinal)))
        {
            throw new ContentLoadException(
                $"{source.Description} cannot insert duplicate id '{id}' at '{target.Address}{path}'.");
        }
    }

    private static void ValidateListIds(
        JsonArray list,
        RawTarget target,
        string path,
        PatchSource source)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in list)
        {
            if (node is not JsonObject item || !item.TryGetPropertyValue("id", out var idNode))
            {
                continue;
            }

            var id = idNode?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ContentLoadException(
                    $"{source.Description} list item at '{target.Address}{path}' has an invalid 'id'.");
            }

            if (!ids.Add(id))
            {
                throw new ContentLoadException($"{source.Description} created duplicate id '{id}' at '{target.Address}{path}'.");
            }
        }
    }

    private static string GetSelectorId(JsonObject operation, string propertyName, PatchSource source)
    {
        var selector = operation[propertyName] as JsonObject
            ?? throw new ContentLoadException(
                $"{source.Description} field '{propertyName}' must be an object containing 'id'.");
        EnsureOnlyProperties(selector, $"{source.Description} field '{propertyName}'", "id");
        return GetRequiredString(selector, "id", source.Description);
    }

    private static void EnsureMergeIdentityUnchanged(
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        ResolvedPath resolved,
        JsonObject patch,
        PatchSource source)
    {
        if (path.Count == 0)
        {
            EnsureIdentityUnchanged(target, patch, source);
            return;
        }

        if (resolved.SelectedId is not null && patch.TryGetPropertyValue("id", out var patchedId) &&
            !string.Equals(patchedId?.GetValue<string>(), resolved.SelectedId, StringComparison.Ordinal))
        {
            throw new ContentLoadException(
                $"{source.Description} cannot change list item identity '{resolved.SelectedId}'.");
        }
    }

    private static void EnsurePropertyIsNotIdentity(
        RawTarget target,
        IReadOnlyList<PatchPathSegment> path,
        ResolvedPath parent,
        string propertyName,
        PatchSource source)
    {
        var rootIdentity = target.Kind == "storySegment" ? "name" : target.Kind == "gameConfig" ? null : "id";
        if ((path.Count == 1 && string.Equals(propertyName, rootIdentity, StringComparison.Ordinal)) ||
            (parent.SelectedId is not null && string.Equals(propertyName, "id", StringComparison.Ordinal)))
        {
            throw new ContentLoadException(
                $"{source.Description} cannot modify identity field '{propertyName}' at '{target.Address}{parent.Address}'.");
        }

        if (path.Count == 1 && target.Kind == "item" &&
            string.Equals(propertyName, "category", StringComparison.Ordinal))
        {
            throw new ContentLoadException(
                $"{source.Description} cannot modify item discriminator 'category' directly. Use a root set operation instead.");
        }
    }

    private static string EscapePointer(string value) => value.Replace("~", "~0").Replace("/", "~1");

    private static void EnsureIdentityUnchanged(RawTarget target, JsonObject patch, PatchSource source)
    {
        var identityProperty = target.Kind == "storySegment" ? "name" : target.Kind == "gameConfig" ? null : "id";
        if (identityProperty is not null && patch.ContainsKey(identityProperty))
        {
            throw new ContentLoadException($"{source.Description} merge cannot modify identity property '{identityProperty}'.");
        }

        if (target.Kind == "item" && patch.ContainsKey("category"))
        {
            throw new ContentLoadException($"{source.Description} merge cannot modify item discriminator 'category'. Use replace instead.");
        }
    }

    private static void EnsureReplacementIdentity(RawTarget target, JsonObject replacement, PatchSource source)
    {
        if (target.Kind == "gameConfig")
        {
            return;
        }

        var propertyName = target.Kind == "storySegment" ? "name" : "id";
        var replacementId = GetRequiredString(replacement, propertyName, source.Description);
        JsonContentLoader.Ensure(string.Equals(target.Id, replacementId, StringComparison.Ordinal),
            $"{source.Description} replacement must preserve {propertyName} '{target.Id}'.");
    }

    private sealed record PatchPathSegment(string? PropertyName, string? Id)
    {
        public static PatchPathSegment ForProperty(string propertyName) => new(propertyName, null);
        public static PatchPathSegment ForId(string id) => new(null, id);
    }

    private sealed record ResolvedPath(JsonNode? Node, string Address, string? SelectedId);

    private sealed record ListItem(JsonObject? Node, int Index);

    private static ContentPackage BuildPackage(RawContentCatalog catalog)
    {
        var packageNode = new JsonObject();
        foreach (var spec in ContentTypeCatalog.All)
        {
            packageNode[spec.PackagePropertyName] = catalog.DrainDefinitions(spec.Kind);
        }

        try
        {
            var package = packageNode.Deserialize<ContentPackage>(JsonContentLoader.ContentJson)
                ?? throw new ContentLoadException("Unable to deserialize content package.");
            package.StoryScripts = catalog.BuildStoryScripts();
            return package;
        }
        catch (JsonException exception)
        {
            throw new ContentLoadException($"Unable to deserialize content package: {exception.Message}", exception);
        }
    }

    private static T DeserializeRequired<T>(JsonObject? value, string description)
    {
        if (value is null)
        {
            throw new ContentLoadException($"Required content '{description}' was not loaded.");
        }

        try
        {
            return value.Deserialize<T>(JsonContentLoader.ContentJson)
                ?? throw new ContentLoadException($"Unable to deserialize '{description}'.");
        }
        catch (JsonException exception)
        {
            throw new ContentLoadException($"Unable to deserialize '{description}': {exception.Message}", exception);
        }
    }

    private static void ValidateConfig(GameConfig config)
    {
        JsonContentLoader.Ensure(!string.IsNullOrWhiteSpace(config.InitialStorySegmentId),
            "Game config is missing initialStorySegmentId.");
        JsonContentLoader.Ensure(config.InitialPartyCharacterIds.Count > 0,
            "Game config is missing initialPartyCharacterIds.");
        JsonContentLoader.Ensure(config.SelectablePortraitIds.Count > 0,
            "Game config is missing selectablePortraitIds.");
        JsonContentLoader.Ensure(config.EquipmentRandomAffixCountWeights.Count > 0,
            "Game config is missing equipmentRandomAffixCountWeights.");
        JsonContentLoader.Ensure(config.BattleGridWidth >= 1 && config.BattleGridHeight >= 1,
            "Game config battle grid dimensions must be positive.");
        var counts = new HashSet<int>();
        var totalWeight = 0;
        foreach (var entry in config.EquipmentRandomAffixCountWeights)
        {
            JsonContentLoader.Ensure(entry.Count > 0,
                "Equipment random affix count must be positive.");
            JsonContentLoader.Ensure(entry.Weight > 0,
                $"Equipment random affix count '{entry.Count}' must have positive weight.");
            JsonContentLoader.Ensure(counts.Add(entry.Count),
                $"Equipment random affix count '{entry.Count}' is duplicated.");
            totalWeight = checked(totalWeight + entry.Weight);
        }
    }

    private static JsonNode ParseNode(string path)
    {
        var json = File.ReadAllText(path);
        return JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            })
            ?? throw new ContentLoadException($"JSON file '{path}' is empty.");
    }

    private static string GetRequiredString(JsonObject value, string propertyName, string source)
    {
        if (value[propertyName] is not JsonValue property ||
            !property.TryGetValue<string>(out var result) ||
            string.IsNullOrWhiteSpace(result))
        {
            throw new ContentLoadException($"'{source}' is missing non-empty string '{propertyName}'.");
        }

        return result;
    }

    private static void EnsureOnlyProperties(JsonObject value, string source, params string[] allowedNames)
    {
        var allowed = allowedNames.ToHashSet(StringComparer.Ordinal);
        foreach (var propertyName in value.Select(static pair => pair.Key))
        {
            if (!allowed.Contains(propertyName))
            {
                throw new ContentLoadException($"Unsupported property '{propertyName}' in '{source}'.");
            }
        }
    }

    private static string Format(JsonNode? value) => value?.ToJsonString() ?? "null";

    private sealed record PatchSource(string ModId, string FilePath, int OperationIndex)
    {
        public string Description => $"mod '{ModId}', patch '{FilePath}', operation {OperationIndex}";
    }

    private sealed record RawTarget(string Kind, string? Id, JsonObject Node, string Address, string OwnerKey);

    private sealed class RawContentCatalog
    {
        private readonly Dictionary<string, OrderedDictionary<string, JsonObject>> _definitions =
            ContentTypeCatalog.All.ToDictionary(static spec => spec.Kind, static _ => new OrderedDictionary<string, JsonObject>(StringComparer.Ordinal), StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> _definitionSources =
            ContentTypeCatalog.All.ToDictionary(static spec => spec.Kind, static _ => new Dictionary<string, string>(StringComparer.Ordinal), StringComparer.Ordinal);
        private readonly Dictionary<string, StoryDocument> _stories = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _segmentOwners = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PatchWrite> _writes = new(StringComparer.Ordinal);

        public JsonObject? GameConfig { get; private set; }
        public List<ContentLoadWarning> Warnings { get; } = [];

        public void AddDefinition(string kind, string id, JsonObject value, string modId, string filePath)
        {
            if (!_definitions.TryGetValue(kind, out var definitions))
            {
                throw new ContentLoadException($"Unknown content kind '{kind}'.");
            }

            if (!definitions.TryAdd(id, value))
            {
                var previousFilePath = _definitionSources[kind][id];
                throw new ContentLoadException(
                    $"Definition '{kind}:{id}' from mod '{modId}' in '{filePath}' conflicts with the definition loaded from '{previousFilePath}'. Use a patch to modify existing content.");
            }

            _definitionSources[kind].Add(id, filePath);
        }

        public void AddStoryScript(string scriptId, JsonObject script, string modId, string filePath)
        {
            if (_stories.ContainsKey(scriptId))
            {
                throw new ContentLoadException($"Story script '{scriptId}' from mod '{modId}' is duplicated.");
            }

            var segments = script["segments"] as JsonArray
                ?? throw new ContentLoadException($"Story script '{filePath}' is missing array 'segments'.");
            foreach (var node in segments)
            {
                var segment = node as JsonObject
                    ?? throw new ContentLoadException($"Story script '{filePath}' contains a non-object segment.");
                var segmentId = GetRequiredString(segment, "name", filePath);
                if (!_segmentOwners.TryAdd(segmentId, scriptId))
                {
                    throw new ContentLoadException($"Story segment '{segmentId}' from mod '{modId}' is duplicated.");
                }
            }

            _stories.Add(scriptId, new StoryDocument(script, modId, filePath));
        }

        public void SetGameConfig(JsonObject config, string modId, string filePath)
        {
            if (GameConfig is not null)
            {
                throw new ContentLoadException($"Game config from mod '{modId}' in '{filePath}' is duplicated.");
            }

            GameConfig = config;
        }

        public RawTarget GetTarget(string kind, string? id, PatchSource source)
        {
            if (kind == "gameConfig")
            {
                JsonContentLoader.Ensure(id is null, $"{source.Description} gameConfig target must not contain id.");
                return new RawTarget(kind, null, GameConfig
                    ?? throw new ContentLoadException($"{source.Description} gameConfig target is not loaded."), "gameConfig", "gameConfig");
            }

            if (kind == "storySegment")
            {
                JsonContentLoader.Ensure(!string.IsNullOrWhiteSpace(id), $"{source.Description} storySegment target requires id.");
                if (!_segmentOwners.TryGetValue(id!, out var scriptId))
                {
                    throw new ContentLoadException($"{source.Description} story segment '{id}' does not exist.");
                }

                var segment = GetStorySegment(scriptId, id!);
                return new RawTarget(kind, id, segment, $"storySegment:{id}", $"story:{scriptId}:{id}");
            }

            if (!_definitions.TryGetValue(kind, out var definitions))
            {
                throw new ContentLoadException($"{source.Description} uses unknown target kind '{kind}'.");
            }

            JsonContentLoader.Ensure(!string.IsNullOrWhiteSpace(id), $"{source.Description} target kind '{kind}' requires id.");
            if (!definitions.TryGetValue(id!, out var definition))
            {
                throw new ContentLoadException($"{source.Description} target '{kind}:{id}' does not exist.");
            }

            return new RawTarget(kind, id, definition, $"{kind}:{id}", $"definition:{kind}:{id}");
        }

        public void ReplaceTarget(RawTarget target, JsonObject replacement, PatchSource source)
        {
            RecordWrite(target, string.Empty, target.Node, replacement, source);
            if (target.Kind == "gameConfig")
            {
                GameConfig = replacement;
                return;
            }

            if (target.Kind == "storySegment")
            {
                var scriptId = _segmentOwners[target.Id!];
                var segments = _stories[scriptId].Node["segments"]!.AsArray();
                var index = FindStorySegmentIndex(segments, target.Id!);
                segments[index] = replacement;
                return;
            }

            _definitions[target.Kind][target.Id!] = replacement;
        }

        public void RemoveTarget(RawTarget target, PatchSource source)
        {
            if (target.Kind == "gameConfig")
            {
                throw new ContentLoadException($"{source.Description} cannot remove gameConfig.");
            }

            RecordWrite(target, string.Empty, target.Node, null, source);
            if (target.Kind == "storySegment")
            {
                var scriptId = _segmentOwners.Remove(target.Id!, out var owner)
                    ? owner
                    : throw new ContentLoadException($"{source.Description} story segment '{target.Id}' does not exist.");
                var segments = _stories[scriptId].Node["segments"]!.AsArray();
                segments.RemoveAt(FindStorySegmentIndex(segments, target.Id!));
                return;
            }

            _definitions[target.Kind].Remove(target.Id!);
            _definitionSources[target.Kind].Remove(target.Id!);
        }

        public JsonArray DrainDefinitions(string kind)
        {
            var array = new JsonArray();
            foreach (var value in _definitions[kind].Values)
            {
                if (value.Parent is not null)
                {
                    throw new ContentLoadException(
                        $"Definition '{kind}:{GetRequiredString(value, "id", kind)}' is still attached to a JSON parent during materialization.");
                }

                array.Add(value);
            }

            _definitions[kind].Clear();
            _definitionSources[kind].Clear();
            return array;
        }

        public Dictionary<string, StoryScript> BuildStoryScripts()
        {
            var result = new Dictionary<string, StoryScript>(StringComparer.Ordinal);
            foreach (var (scriptId, story) in _stories)
            {
                result.Add(scriptId, StoryScriptJson.Parse(story.Node, story.FilePath));
            }

            return result;
        }

        public void RecordWrite(
            RawTarget target,
            string path,
            JsonNode? previous,
            JsonNode? current,
            PatchSource source)
        {
            var key = target.OwnerKey + path;
            if (_writes.TryGetValue(key, out var prior) &&
                !string.Equals(prior.Source.ModId, source.ModId, StringComparison.Ordinal) &&
                !JsonNode.DeepEquals(prior.Value, current))
            {
                Warnings.Add(new ContentLoadWarning(
                    target.Address + path,
                    prior.Source.ModId,
                    source.ModId,
                    $"Mod '{source.ModId}' overrides a value previously written by mod '{prior.Source.ModId}' at '{target.Address}{path}'."));
            }

            _writes[key] = new PatchWrite(source, current?.DeepClone());
        }

        public void RecordAppend(RawTarget target, string path, PatchSource source)
        {
            _writes[target.OwnerKey + path + "/$append/" + source.ModId] = new PatchWrite(source, null);
        }

        private JsonObject GetStorySegment(string scriptId, string segmentId)
        {
            var segments = _stories[scriptId].Node["segments"]!.AsArray();
            return segments[FindStorySegmentIndex(segments, segmentId)]!.AsObject();
        }

        private static int FindStorySegmentIndex(JsonArray segments, string segmentId)
        {
            for (var index = 0; index < segments.Count; index++)
            {
                if (segments[index] is JsonObject segment &&
                    string.Equals(segment["name"]?.GetValue<string>(), segmentId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            throw new ContentLoadException($"Story segment '{segmentId}' was indexed but not found.");
        }

        private sealed record StoryDocument(JsonObject Node, string ModId, string FilePath);
        private sealed record PatchWrite(PatchSource Source, JsonNode? Value);
    }
}

public sealed class ContentLoadException : InvalidOperationException
{
    public ContentLoadException(string message) : base(message)
    {
    }

    public ContentLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
