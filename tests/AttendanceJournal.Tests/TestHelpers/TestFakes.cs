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