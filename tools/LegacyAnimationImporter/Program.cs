using System.Text;
using System.Text.RegularExpressions;

if (args.Length < 2)
{
	Console.Error.WriteLine("Usage: LegacyAnimationImporter <legacy-jyxr-root> <workspace-root>");
	return 1;
}

var sourceRoot = Path.GetFullPath(args[0]);
var workspaceRoot = Path.GetFullPath(args[1]);

var skillSceneDirectory = Path.Combine(sourceRoot, "asset", "animation", "skill");
var combatantSceneDirectory = Path.Combine(sourceRoot, "asset", "animation", "combatant");
var skillDestinationDirectory = Path.Combine(workspaceRoot, "assets", "animation", "skill");
var combatantDestinationDirectory = Path.Combine(workspaceRoot, "assets", "animation", "combatant");

Directory.CreateDirectory(skillDestinationDirectory);
Directory.CreateDirectory(combatantDestinationDirectory);

var copiedAtlasResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var importedSkillCount = 0;
var importedCombatantCount = 0;

foreach (var scenePath in Directory.GetFiles(skillSceneDirectory, "*.tscn", SearchOption.TopDirectoryOnly))
{
	var animationId = Path.GetFileNameWithoutExtension(scenePath);
	var animationLibrary = LegacySceneParser.ParseAnimationLibrary(scenePath);
	File.WriteAllText(
		Path.Combine(skillDestinationDirectory, $"{animationId}.tres"),
		animationLibrary.ResourceText,
		new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	CopyDependencies(animationLibrary.TextureResourcePaths, sourceRoot, workspaceRoot, copiedAtlasResources);
	importedSkillCount += 1;
}

foreach (var scenePath in Directory.GetFiles(combatantSceneDirectory, "*.tscn", SearchOption.TopDirectoryOnly))
{
	var animationId = Path.GetFileNameWithoutExtension(scenePath);
	var animationLibrary = LegacySceneParser.ParseAnimationLibrary(scenePath);
	File.WriteAllText(
		Path.Combine(combatantDestinationDirectory, $"{animationId}.tres"),
		animationLibrary.ResourceText,
		new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	CopyDependencies(animationLibrary.TextureResourcePaths, sourceRoot, workspaceRoot, copiedAtlasResources);
	importedCombatantCount += 1;
}

Console.WriteLine($"Imported skill animations: {importedSkillCount}");
Console.WriteLine($"Imported combatant animations: {importedCombatantCount}");
Console.WriteLine($"Copied atlas textures: {copiedAtlasResources.Count}");
return 0;

static void CopyDependencies(
	IReadOnlyList<string> textureResourcePaths,
	string sourceRoot,
	string workspaceRoot,
	HashSet<string> copiedAtlasResources)
{
	foreach (var textureResourcePath in textureResourcePaths.Distinct(StringComparer.Ordinal))
	{
		var normalizedTexturePath = NormalizeResourcePath(textureResourcePath);
		if (!copiedAtlasResources.Add(normalizedTexturePath))
		{
			continue;
		}

		var sourceTexturePath = ResolveSourcePath(sourceRoot, normalizedTexturePath);
		var destinationTexturePath = ResolveDestinationPath(workspaceRoot, normalizedTexturePath);
		Directory.CreateDirectory(Path.GetDirectoryName(destinationTexturePath)!);

		var textureResourceText = File.ReadAllText(sourceTexturePath);
		var atlasResourcePaths = LegacySceneParser.ParseAtlasDependencies(textureResourceText)
			.Select(NormalizeResourcePath)
			.ToArray();
		var rewrittenTextureResourceText = RewriteResourceText(textureResourceText);
		File.WriteAllText(destinationTexturePath, rewrittenTextureResourceText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		foreach (var atlasResourcePath in atlasResourcePaths)
		{
			var sourceAtlasPath = ResolveSourcePath(sourceRoot, atlasResourcePath);
			var destinationAtlasPath = ResolveDestinationPath(workspaceRoot, atlasResourcePath);
			Directory.CreateDirectory(Path.GetDirectoryName(destinationAtlasPath)!);
			File.Copy(sourceAtlasPath, destinationAtlasPath, overwrite: true);
		}
	}
}

static string ResolveSourcePath(string sourceRoot, string resourcePath)
{
	if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
	{
		throw new InvalidOperationException($"Unsupported resource path '{resourcePath}'.");
	}

	var relativePath = resourcePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
	var sourcePath = Path.Combine(sourceRoot, relativePath);
	if (File.Exists(sourcePath))
	{
		return sourcePath;
	}

	if (resourcePath.StartsWith("res://assets/", StringComparison.Ordinal))
	{
		var legacyRelativePath = resourcePath["res://assets/".Length..].Replace('/', Path.DirectorySeparatorChar);
		var legacySourcePath = Path.Combine(sourceRoot, "asset", legacyRelativePath);
		if (File.Exists(legacySourcePath))
		{
			return legacySourcePath;
		}
	}

	return sourcePath;
}

static string ResolveDestinationPath(string workspaceRoot, string resourcePath)
{
	if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
	{
		throw new InvalidOperationException($"Unsupported resource path '{resourcePath}'.");
	}

	var destinationResourcePath = RewriteResourcePath(resourcePath);
	var relativePath = destinationResourcePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
	return Path.Combine(workspaceRoot, relativePath);
}

static string RewriteResourcePath(string resourcePath)
{
	var normalizedResourcePath = NormalizeResourcePath(resourcePath);
	return normalizedResourcePath.Replace("res://asset/", "res://assets/", StringComparison.Ordinal);
}

static string RewriteResourceText(string text)
{
	ArgumentNullException.ThrowIfNull(text);

	return Regex.Replace(
			text.Replace("res://asset/", "res://assets/", StringComparison.Ordinal),
			" uid=\"uid://[^\"]+\"",
			string.Empty,
			RegexOptions.CultureInvariant)
		.Replace("\r\n", "\n", StringComparison.Ordinal);
}

static string NormalizeResourcePath(string resourcePath) =>
	resourcePath.Trim().Replace('\\', '/');

sealed record ImportedAnimationLibrary(string ResourceText, IReadOnlyList<string> TextureResourcePaths);

sealed record ImportedAnimationBlock(string Id, string Name, string BlockText);

static class LegacySceneParser
{
	private const string AnimationTargetNodeName = "Sprite";

	private static readonly Regex TextureExtResourceRegex = new(
		"^\\[ext_resource type=\"Texture2D\".* path=\"(?<path>res://[^\"]+)\" id=\"(?<id>[^\"]+)\"\\]$",
		RegexOptions.Multiline | RegexOptions.CultureInvariant);

	private static readonly Regex AnimationBlockRegex = new(
		"(?<header>\\[sub_resource type=\"Animation\" id=\"(?<id>[^\"]+)\"\\])\\s*(?<body>.*?)(?=\\n\\[sub_resource|\\n\\[node|\\z)",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);

	private static readonly Regex ResourceNameRegex = new(
		"resource_name = \"(?<name>[^\"]+)\"",
		RegexOptions.CultureInvariant);

	private static readonly Regex TrackPathRegex = new(
		"tracks/\\d+/path = NodePath\\(\"(?<path>[^\"]+)\"\\)",
		RegexOptions.CultureInvariant);

	public static ImportedAnimationLibrary ParseAnimationLibrary(string scenePath)
	{
		var text = SanitizeSceneText(File.ReadAllText(scenePath));
		var extResources = TextureExtResourceRegex.Matches(text)
			.Select(match => match.Value.TrimEnd())
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		var animationBlocks = AnimationBlockRegex.Matches(text)
			.Select(match =>
			{
				var blockText = $"{match.Groups["header"].Value}\n{NormalizeAnimationBlock(match.Groups["body"].Value.TrimEnd())}";
				var name = ResourceNameRegex.Match(match.Groups["body"].Value).Groups["name"].Value;
				if (string.IsNullOrWhiteSpace(name))
				{
					throw new InvalidOperationException($"Animation block in '{scenePath}' is missing resource_name.");
				}

				return new ImportedAnimationBlock(
					match.Groups["id"].Value,
					name,
					blockText);
			})
			.ToArray();

		if (animationBlocks.Length == 0)
		{
			throw new InvalidOperationException($"No animation blocks found in '{scenePath}'.");
		}

		var textureResourcePaths = TextureExtResourceRegex.Matches(text)
			.Select(match => match.Groups["path"].Value)
			.ToArray();

		var builder = new StringBuilder();
		builder.AppendLine("[gd_resource type=\"AnimationLibrary\" format=3]");
		builder.AppendLine();
		foreach (var extResource in extResources)
		{
			builder.AppendLine(extResource);
		}

		builder.AppendLine();
		foreach (var animationBlock in animationBlocks)
		{
			builder.AppendLine(animationBlock.BlockText);
			builder.AppendLine();
		}

		builder.AppendLine("[resource]");
		builder.AppendLine("_data = {");
		for (var index = 0; index < animationBlocks.Length; index++)
		{
			var animationBlock = animationBlocks[index];
			var suffix = index + 1 < animationBlocks.Length ? "," : string.Empty;
			builder.AppendLine($"&\"{animationBlock.Name}\": SubResource(\"{animationBlock.Id}\"){suffix}");
		}
		builder.AppendLine("}");

		return new ImportedAnimationLibrary(builder.ToString(), textureResourcePaths);
	}

	public static IEnumerable<string> ParseAtlasDependencies(string resourceText) =>
		TextureExtResourceRegex.Matches(resourceText)
			.Select(match => match.Groups["path"].Value);

	private static string SanitizeSceneText(string text)
	{
		return Regex.Replace(
				text.Replace("res://asset/", "res://assets/", StringComparison.Ordinal),
				" uid=\"uid://[^\"]+\"",
				string.Empty,
				RegexOptions.CultureInvariant)
			.Replace("\r\n", "\n", StringComparison.Ordinal);
	}

	private static string NormalizeAnimationBlock(string blockText) =>
		TrackPathRegex.Replace(
			blockText,
			static match =>
			{
				var originalPath = match.Groups["path"].Value;
				var normalizedPath = NormalizeTrackPath(originalPath);
				return string.Equals(originalPath, normalizedPath, StringComparison.Ordinal)
					? match.Value
					: match.Value.Replace(originalPath, normalizedPath, StringComparison.Ordinal);
			});

	private static string NormalizeTrackPath(string originalPath)
	{
		if (string.IsNullOrWhiteSpace(originalPath))
		{
			return originalPath;
		}

		var propertySeparatorIndex = originalPath.IndexOf(':');
		var nodePath = propertySeparatorIndex >= 0
			? originalPath[..propertySeparatorIndex]
			: originalPath;
		var propertyPath = propertySeparatorIndex >= 0
			? originalPath[propertySeparatorIndex..]
			: string.Empty;

		var segments = nodePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (segments.Length == 0)
		{
			return originalPath;
		}

		var targetNodeName = segments[^1];
		if (targetNodeName is "." or "..")
		{
			return originalPath;
		}

		return string.Equals(targetNodeName, AnimationTargetNodeName, StringComparison.Ordinal)
			? $"{AnimationTargetNodeName}{propertyPath}"
			: $"{targetNodeName}{propertyPath}";
	}
}
