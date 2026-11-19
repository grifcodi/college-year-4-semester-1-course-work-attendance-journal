using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Data;

internal static class DatabaseInitializer
{
    public static string GetDatabasePath()
    {
        var folder = Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
             "AttendanceJournal");

        return Path.Combine(folder, "journal.db");
    }
    public static string GetConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(),
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 5,
        };

        return builder.ToString();
    }

    public static async Task InitializeAsync(IDbContextFactory<AppDbContext> contextFactory)
    {
        var dbPath = GetDatabasePath();
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var context = await contextFactory.CreateDbContextAsync();

        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }
}
