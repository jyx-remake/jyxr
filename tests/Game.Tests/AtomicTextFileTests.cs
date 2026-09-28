using Game.Application;

namespace Game.Tests;

public sealed class AtomicTextFileTests
{
    [Fact]
    public void ReplacesCompleteFile_AndPreservesExistingFileWhenReplacementFails()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "persistence-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "profile.json");
        try
        {
            AtomicTextFile.Write(path, "old");
            AtomicTextFile.Write(path, "新档案");
            Assert.Equal("新档案", File.ReadAllText(path));
            // On Windows an open reader without delete sharing prevents replacement.
            if (OperatingSystem.IsWindows())
            {
                using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var failure = Record.Exception(() => AtomicTextFile.Write(path, "incomplete"));
                Assert.True(failure is IOException or UnauthorizedAccessException);
                Assert.Equal("新档案", File.ReadAllText(path));
            }
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
