using Game.Content.Loading;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Game.ContentValidator <runtime-data-directory>");
    return 2;
}

var directory = Path.GetFullPath(args[0]);
try
{
    var repository = new JsonContentLoader().LoadFromDirectory(directory);
    Console.WriteLine(
        $"CONTENT_VALIDATION_OK maps={repository.Maps.Count} " +
        $"characters={repository.Characters.Count} stories={repository.StorySegments.Count} " +
        $"titles={repository.CharacterTitles.Count}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"CONTENT_VALIDATION_FAILED {directory}");
    Console.Error.WriteLine(exception);
    return 1;
}
