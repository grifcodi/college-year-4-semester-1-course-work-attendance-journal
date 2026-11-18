using AttendanceJournal.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests.TestHelpers;

public abstract class SqliteInMemoryTestBase : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    protected IDbContextFactory<AppDbContext> ContextFactory { get; }

    protected SqliteInMemoryTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        ContextFactory = new SingleConnectionDbContextFactory(options);
    }

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await _connection.OpenAsync(ct);
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        await context.Database.MigrateAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class SingleConnectionDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
