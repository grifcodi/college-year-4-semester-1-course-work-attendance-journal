using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class AuditLogService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), IAuditLogService
{
    public const int MaxEntriesLimit = 5000;

    public async Task<AuditLogPage> GetEntriesAsync(AuditLogFilter filter)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        IQueryable<AuditEntry> query = context.AuditEntries.AsNoTracking();

        if (filter.FromDate is DateOnly from)
        {
            var fromUtc = ToUtc(from);
            query = query.Where(a => a.TimeStamp >= fromUtc);
        }

        if (filter.ToDate is DateOnly to)
        {
            // Inclusive end day: everything before the next local midnight.
            var toUtcExclusive = ToUtc(to.AddDays(1));
            query = query.Where(a => a.TimeStamp < toUtcExclusive);
        }

        if (filter.EntityType is AuditEntityType entityType)
            query = query.Where(a => a.EntityType == entityType);

        if (filter.Action is AuditAction action)
            query = query.Where(a => a.Action == action);

        var total = await query.CountAsync();
        var limit = Math.Clamp(filter.MaxEntries, 1, MaxEntriesLimit);

        // Order is given by the primary key, not by time (spec 5.1).
        var entries = await query
            .OrderByDescending(a => a.AuditEntryId)
            .Take(limit)
            .Select(a => new AuditEntryDto(
                a.AuditEntryId,
                a.TimeStamp,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.PayloadJson))
            .ToListAsync();

        return new AuditLogPage(
            [.. entries.Select(e => e with { TimeStampUtc = DateTime.SpecifyKind(e.TimeStampUtc, DateTimeKind.Utc) })],
            total);
    }

    private static DateTime ToUtc(DateOnly localDate) =>
        localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
}
