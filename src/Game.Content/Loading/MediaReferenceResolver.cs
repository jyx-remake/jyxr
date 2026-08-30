using Game.Core.Abstractions;

namespace Game.Content.Loading;

public enum MediaAssetKind
{
    Texture,
    Audio,
    Video,
}

public enum MediaReferenceKind
{
    ResourceId,
    Path,
}

public sealed record MediaReferenceResolution(
    bool IsSuccess,
    string Reference,
    MediaAssetKind AssetKind,
    MediaReferenceKind? ReferenceKind,
    string? AssetPath,
    string? Error)
{
    public static MediaReferenceResolution Success(
        string reference,
        MediaAssetKind assetKind,
        MediaReferenceKind referenceKind,
        string assetPath) =>
        new(true, reference, assetKind, referenceKind, assetPath, null);

    public static MediaReferenceResolution Failure(
        string reference,
        MediaAssetKind assetKind,
        MediaReferenceKind? referenceKind,
        string error) =>
        new(false, reference, assetKind, referenceKind, null, error);
}

public static class MediaReferenceResolver
{
    private static readonly IReadOnlyDictionary<MediaAssetKind, string[]> SupportedExtensions =
        new Dictionary<MediaAssetKind, string[]>
        {
            [MediaAssetKind.Texture] = [".png", ".jpg", ".jpeg", ".webp", ".tres", ".res"],
            [MediaAssetKind.Audio] = [".ogg", ".mp3", ".wav", ".flac"],
            [MediaAssetKind.Video] = [".ogv"],
        };

    public static MediaReferenceResolution Resolve(
        string? reference,
        MediaAssetKind assetKind,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);

        var normalizedReference = reference?.Trim() ?? string.Empty;
        if (normalizedReference.Length == 0)
        {
            return MediaReferenceResolution.Failure(
                normalizedReference,
                assetKind,
                null,
                "Reference is empty.");
        }

        if (normalizedReference.Contains('\\'))
        {
            return MediaReferenceResolution.Failure(
                normalizedReference,
                assetKind,
                null,
                "References must use forward slashes.");
        }

        if (normalizedReference.Contains('/'))
        {
            return ResolvePath(normalizedReference, normalizedReference, assetKind, MediaReferenceKind.Path);
        }

        if (!contentRepository.TryGetResource(normalizedReference, out var resource))
        {
            return MediaReferenceResolution.Failure(
                normalizedReference,
                assetKind,
                MediaReferenceKind.ResourceId,
                $"Resource ID '{normalizedReference}' does not exist.");
        }

        return ResolvePath(
            normalizedReference,
            resource.Value.Trim(),
            assetKind,
            MediaReferenceKind.ResourceId);
    }

    public static IReadOnlyList<string> GetCandidateAssetPaths(
        string assetPath,
        MediaAssetKind assetKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        var fileName = assetPath.Split('/')[^1];
        if (Path.HasExtension(fileName))
        {
            return [assetPath];
        }

        return SupportedExtensions[assetKind]
            .Select(extension => $"{assetPath}{extension}")
            .ToArray();
    }

    private static MediaReferenceResolution ResolvePath(
        string originalReference,
        string path,
        MediaAssetKind assetKind,
        MediaReferenceKind referenceKind)
    {
        var error = ValidatePath(path, assetKind);
        if (error is not null)
        {
            return MediaReferenceResolution.Failure(
                originalReference,
                assetKind,
                referenceKind,
                referenceKind == MediaReferenceKind.ResourceId
                    ? $"Resource ID '{originalReference}' has invalid value '{path}': {error}"
                    : error);
        }

        var assetPath = assetKind == MediaAssetKind.Texture ? $"art/{path}" : path;
        return MediaReferenceResolution.Success(originalReference, assetKind, referenceKind, assetPath);
    }

    private static string? ValidatePath(string path, MediaAssetKind assetKind)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Path is empty.";
        }

        if (path.Contains('\\'))
        {
            return "Path must use forward slashes.";
        }

        if (path.StartsWith('/') || path.Contains(':'))
        {
            return "Absolute paths and URI schemes are not allowed.";
        }

        var segments = path.Split('/');
        if (segments.Length < 2 || segments.Any(static segment =>
                segment.Length == 0 || segment is "." or ".."))
        {
            return "Path must contain valid non-relative segments separated by '/'.";
        }

        var firstSegment = segments[0];
        switch (assetKind)
        {
            case MediaAssetKind.Texture when firstSegment.Equals("assets", StringComparison.OrdinalIgnoreCase) ||
                                                 firstSegment.Equals("art", StringComparison.OrdinalIgnoreCase) ||
                                                 firstSegment.Equals("audio", StringComparison.OrdinalIgnoreCase) ||
                                                 firstSegment.Equals("video", StringComparison.OrdinalIgnoreCase):
                return $"Texture path cannot start with reserved segment '{firstSegment}'.";
            case MediaAssetKind.Audio when !firstSegment.Equals("audio", StringComparison.Ordinal):
                return "Audio path must start with 'audio/'.";
            case MediaAssetKind.Video when !firstSegment.Equals("video", StringComparison.OrdinalIgnoreCase) &&
                                             !firstSegment.Equals("MV", StringComparison.OrdinalIgnoreCase):
                return "Video path must start with 'video/' or legacy 'MV/'.";
        }

        var extension = Path.GetExtension(segments[^1]);
        if (extension.Length > 0 &&
            !SupportedExtensions[assetKind].Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"Extension '{extension}' is not supported for {assetKind}.";
        }

        return null;
    }
}
