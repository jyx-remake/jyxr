using System.Text.Json;
using Game.Core.Definitions;
using Game.Core.Serialization;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    public InMemoryContentRepository LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var package = JsonSerializer.Deserialize<ContentPackage>(json, GameJson.Default)
            ?? throw new InvalidOperationException("Unable to deserialize content package.");
        return LoadFromPackage(package);
    }

    public InMemoryContentRepository LoadFromDirectory(string directoryPath) =>
        LoadFromPackage(LoadPackageFromDirectory(directoryPath));

    public InMemoryContentRepository LoadFromPackage(ContentPackage package)
    {
        var repository = BuildRepository(package);
        ValidateRepository(repository);
        return repository;
    }

    internal static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
