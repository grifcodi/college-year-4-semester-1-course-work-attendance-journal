using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceJournal.Data;

internal static class DatabaseInitializer
{
    public static string GetConnectionString()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AttendanceJournal");

        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "journal.db");

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 5,
        };

        return builder.ToString();
    }

    public static async Task InitializeAsync(IDbContextFactory<AppDbContext> contextFactory)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }
}
