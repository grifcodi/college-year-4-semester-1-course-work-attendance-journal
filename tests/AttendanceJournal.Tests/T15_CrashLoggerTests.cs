using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Tests;

public sealed class T15_CrashLoggerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aj-logs-" + Guid.NewGuid().ToString("N"));

    private string LogPath => Path.Combine(_dir, CrashLogger.FileName);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private void Log(string context, Exception exception, string? userProfilePath = null, long maxBytes = 1_000_000)
    {
        var failure = CrashLogger.TryLog(context, exception, _dir, userProfilePath, maxBytes);
        Assert.Null(failure);
    }

    [Fact]
    public void Log_WritesContextTypeAndMessage()
    {
        Log("Startup", new InvalidOperationException("Збій ініціалізації"));

        var text = File.ReadAllText(LogPath);
        Assert.Contains("Startup", text);
        Assert.Contains("InvalidOperationException", text);
        Assert.Contains("Збій ініціалізації", text);
    }

    [Fact]
    public void Log_CreatesDirectoryWhenMissing()
    {
        Assert.False(Directory.Exists(_dir));

        Log("Startup", new Exception("x"));

        Assert.True(File.Exists(LogPath));
    }

    [Fact]
    public void Log_StartsEntryWithUtcTimestamp()
    {
        Log("Startup", new Exception("x"));

        var firstLine = File.ReadLines(LogPath).First();
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2}T[\d:.]+Z\] Startup$", firstLine);
    }

    [Fact]
    public void Log_AppendsInsteadOfOverwriting()
    {
        Log("First", new Exception("перша помилка"));
        Log("Second", new Exception("друга помилка"));

        var text = File.ReadAllText(LogPath);
        Assert.Contains("перша помилка", text);
        Assert.Contains("друга помилка", text);
    }

    [Fact]
    public void Log_IncludesInnerException()
    {
        var ex = new Exception("outer", new InvalidOperationException("inner-details"));

        Log("Startup", ex);

        Assert.Contains("inner-details", File.ReadAllText(LogPath));
    }

    [Fact]
    public void Log_ReplacesUserProfilePath_WithPlaceholder()
    {
        var ex = new IOException(@"Cannot open C:\Users\Taras\AppData\Local\AttendanceJournal\journal.db");

        Log("Startup", ex, userProfilePath: @"C:\Users\Taras");

        var text = File.ReadAllText(LogPath);
        Assert.DoesNotContain("Taras", text);
        Assert.Contains(@"%USERPROFILE%\AppData\Local\AttendanceJournal\journal.db", text);
    }

    [Fact]
    public void Log_ReplacesUserProfilePath_IgnoringCase()
    {
        var ex = new IOException(@"Cannot open c:\users\taras\journal.db");

        Log("Startup", ex, userProfilePath: @"C:\Users\Taras");

        var text = File.ReadAllText(LogPath);
        Assert.DoesNotContain("taras", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%USERPROFILE%", text);
    }

    [Fact]
    public void Log_NeverThrows_WhenDirectoryCannotBeCreated()
    {
        Directory.CreateDirectory(_dir);
        var blocker = Path.Combine(_dir, "blocker");
        File.WriteAllText(blocker, "це файл, а не папка");

        var thrown = Record.Exception(() =>
            CrashLogger.LogException("Startup", new Exception("x"), Path.Combine(blocker, "logs")));

        Assert.Null(thrown);
    }

    [Fact]
    public void TryLog_ReturnsTheFailure_WhenDirectoryCannotBeCreated()
    {
        Directory.CreateDirectory(_dir);
        var blocker = Path.Combine(_dir, "blocker");
        File.WriteAllText(blocker, "це файл, а не папка");

        var failure = CrashLogger.TryLog("Startup", new Exception("x"), Path.Combine(blocker, "logs"));

        Assert.NotNull(failure);
    }

    [Fact]
    public void Log_RotatesOversizedFile()
    {
        Log("First", new Exception(new string('a', 200)), maxBytes: 50);
        Log("Second", new Exception("нова помилка"), maxBytes: 50);

        Assert.True(File.Exists(LogPath + ".old"));
        Assert.Contains("First", File.ReadAllText(LogPath + ".old"));
        var current = File.ReadAllText(LogPath);
        Assert.Contains("Second", current);
        Assert.DoesNotContain("First", current);
    }

    [Fact]
    public async Task Log_FromManyThreads_WritesEveryEntry()
    {
        var ct = TestContext.Current.CancellationToken;

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            Task.Run(() => Log("Parallel", new Exception($"помилка-{i}")), ct)));

        var text = File.ReadAllText(LogPath);
        for (var i = 0; i < 20; i++)
            Assert.Contains($"помилка-{i}", text);
    }
}