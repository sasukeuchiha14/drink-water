using DrinkWater.App.Persistence;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class SloganTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DrinkWater.SloganTests", Guid.NewGuid().ToString("N"));
    private readonly SloganStore _store;

    public SloganTests()
    {
        Directory.CreateDirectory(_directory);
        _store = new(Path.Combine(_directory, "slogans.txt"));
    }

    [Fact]
    public void CreatesAnEditableDefaultListOnlyWhenMissing()
    {
        SloganLoadResult loaded = _store.Load();
        Assert.Null(loaded.Warning);
        Assert.Equal(ReminderSlogans.Defaults, loaded.Messages);
        File.WriteAllText(_store.FilePath, "My custom reminder.");
        _store.EnsureExists();
        Assert.Equal("My custom reminder.", File.ReadAllText(_store.FilePath));
    }

    [Fact]
    public void SavedEditsAreReloadedAndCommentsBlanksAndDuplicatesAreIgnored()
    {
        File.WriteAllText(_store.FilePath, "\uFEFF# My list\r\n\r\n Sip now. \r\nSip now.\r\n喝点水。\r\n");
        Assert.Equal(new[] { "Sip now.", "喝点水。" }, _store.Load().Messages);
        File.WriteAllText(_store.FilePath, "An updated reminder.");
        Assert.Equal("An updated reminder.", Assert.Single(_store.Load().Messages));
    }

    [Theory]
    [InlineData("")]
    [InlineData("# Only a comment\n")]
    [InlineData("Bad\u0001control")]
    public void InvalidListsUseDefaultsWithoutOverwritingTheFile(string text)
    {
        File.WriteAllText(_store.FilePath, text);
        SloganLoadResult loaded = _store.Load();
        Assert.NotNull(loaded.Warning);
        Assert.Equal(ReminderSlogans.Defaults, loaded.Messages);
        Assert.Equal(text, File.ReadAllText(_store.FilePath));
    }

    [Fact]
    public void OversizedFilesMessagesAndListsUseSafeDefaults()
    {
        foreach (string text in new[]
        {
            new string('x', SloganStore.MaximumBytes + 1),
            new string('x', SloganStore.MaximumMessageLength + 1),
            string.Join('\n', Enumerable.Range(0, SloganStore.MaximumSlogans + 1).Select(index => $"Slogan {index}"))
        })
        {
            File.WriteAllText(_store.FilePath, text);
            Assert.NotNull(_store.Load().Warning);
            Assert.Equal(text, File.ReadAllText(_store.FilePath));
        }
    }

    [Fact]
    public void InvalidUtf8AndLockedFilesUseDefaults()
    {
        File.WriteAllBytes(_store.FilePath, [0xFF, 0xFE, 0x00]);
        Assert.NotNull(_store.Load().Warning);
        File.WriteAllText(_store.FilePath, "Keep me.");
        using FileStream locked = new(_store.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.NotNull(_store.Load().Warning);
    }

    [Fact]
    public void RandomModeNeverImmediatelyRepeatsWhenThereAreAlternatives()
    {
        ReminderSloganSelector selector = new(new Random(123));
        string? previous = null;
        HashSet<string> seen = [];
        for (int index = 0; index < 100; index++)
        {
            string next = selector.Next(ReminderSlogans.Defaults, randomize: true);
            Assert.Contains(next, ReminderSlogans.Defaults);
            Assert.NotEqual(previous, next);
            seen.Add(next);
            previous = next;
        }

        Assert.True(seen.Count > 1);
    }

    [Fact]
    public void FixedModeUsesTheFirstSloganAndSingleMessageListsAlwaysWork()
    {
        ReminderSloganSelector selector = new(new Random(123));
        Assert.Equal("First", selector.Next(new[] { "First", "Second" }, randomize: false));
        Assert.Equal("Only", selector.Next(new[] { "Only" }, randomize: true));
        Assert.Equal("Only", selector.Next(new[] { "Only" }, randomize: true));
        Assert.Equal("Edited", selector.Next(new[] { "Edited", "Another" }, randomize: false));
    }

    public void Dispose()
    {
        string allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DrinkWater.SloganTests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(_directory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unexpected test directory.");
        }

        Directory.Delete(_directory, recursive: true);
    }
}
