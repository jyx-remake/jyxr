using Game.Application;

namespace Game.Tests;

public sealed class AtomicTextFileTests
{
    [Fact]
    public void BackupContainsPreviousCompleteSnapshot()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "persistence-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "autosave.json");
        var backup = path + ".bak";
        try
        {
            AtomicTextFile.Write(path, "first", backup);
            AtomicTextFile.Write(path, "second", backup);
            AtomicTextFile.Write(path, "third", backup);
            Assert.Equal("third", File.ReadAllText(path));
            Assert.Equal("second", File.ReadAllText(backup));
            if (OperatingSystem.IsWindows())
            {
                using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var failure = Record.Exception(() => AtomicTextFile.Write(path, "incomplete", backup));
                Assert.True(failure is IOException or UnauthorizedAccessException);
                Assert.Equal("third", File.ReadAllText(path));
                Assert.Equal("second", File.ReadAllText(backup));
            }
            Assert.Equal(2, Directory.GetFiles(directory).Length);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

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
