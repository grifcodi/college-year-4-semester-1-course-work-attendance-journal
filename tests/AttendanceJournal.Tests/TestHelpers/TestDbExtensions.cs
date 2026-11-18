using AttendanceJournal.Data;
using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests.TestHelpers;

public static class TestDbExtensions
{
    public static async Task<List<AuditEntry>> GetAuditAsync(
        this IDbContextFactory<AppDbContext> factory,
        AuditEntityType? type = null,
        int? entityId = null)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = await factory.CreateDbContextAsync(ct);

        IQueryable<AuditEntry> query = context.AuditEntries.AsNoTracking();
        if (type is not null)
            query = query.Where(a => a.EntityType == type.Value);
        if (entityId is not null)
            query = query.Where(a => a.EntityId == entityId.Value);

        return await query.OrderBy(a => a.AuditEntryId).ToListAsync(ct);
    }
}