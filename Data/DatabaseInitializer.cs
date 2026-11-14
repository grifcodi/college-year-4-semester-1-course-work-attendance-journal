using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceJournal.Data;

internal class DatabaseInitializer
{
    public static string GetDatabasePath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AttendanceJournal");

        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "journal.db");
    }

    public static async Task InitializeAsync(IDbContextFactory<AppDbContext> contextFactory)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
    }
}
