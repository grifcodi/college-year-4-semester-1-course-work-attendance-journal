using AttendanceJournal.Data;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests.TestHelpers;

public sealed class FakeConfirmationService : IConfirmationService
{
    public bool Answer { get; set; } = true;
    public List<string> Messages { get; } = [];

    public Task<bool> ConfirmAsync(string message)
    {
        Messages.Add(message);
        return Task.FromResult(Answer);
    }
}

public sealed class FakeErrorLog : IErrorLog
{
    public List<(string Context, Exception Exception)> Entries { get; } = [];

    public void Log(string context, Exception exception) => Entries.Add((context, exception));
}
public sealed class ThrowingContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() =>
        throw new InvalidOperationException("Імітація збою бази даних");
}

public static class TestWait
{
    public static async Task UntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10, ct);

        Assert.True(condition(), "Умова не виконалась за відведений час.");
    }
}

/// <summary>
/// File dialogs without a UI. Set <see cref="FileToOpen"/> for import tests; export tests read
/// <see cref="SavedBytes"/> after the view model has written the file.
/// </summary>
public sealed class FakeFileDialogService : AttendanceJournal.ViewModels.Interfaces.IFileDialogService
{
    private MemoryStream? _saved;

    public AttendanceJournal.ViewModels.Interfaces.PickedFile? FileToOpen { get; set; }
    public bool CancelSave { get; set; }
    public string? SuggestedFileName { get; private set; }

    /// <summary>Bytes written by the last save. MemoryStream.ToArray works after the stream is disposed.</summary>
    public byte[] SavedBytes => _saved?.ToArray() ?? [];

    public Task<AttendanceJournal.ViewModels.Interfaces.PickedFile?> PickFileToOpenAsync(
        string title, string fileTypeName, IReadOnlyList<string> extensions) =>
        Task.FromResult(FileToOpen);

    public Task<AttendanceJournal.ViewModels.Interfaces.PickedFile?> PickFileToSaveAsync(
        string title, string suggestedFileName, string fileTypeName, string extension)
    {
        SuggestedFileName = suggestedFileName;
        if (CancelSave)
            return Task.FromResult<AttendanceJournal.ViewModels.Interfaces.PickedFile?>(null);

        _saved = new MemoryStream();
        return Task.FromResult<AttendanceJournal.ViewModels.Interfaces.PickedFile?>(
            new AttendanceJournal.ViewModels.Interfaces.PickedFile(suggestedFileName, () => Task.FromResult<Stream>(_saved)));
    }

    public static AttendanceJournal.ViewModels.Interfaces.PickedFile FromText(string fileName, string content) =>
        new(fileName, () => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))));
}
