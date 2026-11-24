using AttendanceJournal.Models;

namespace AttendanceJournal.Services.Interfaces;

/// <summary>
/// Filter of the audit log viewer (FR-12). Dates are calendar days in local time, both inclusive.
/// Null means "no filter" for that field.
/// </summary>
public record AuditLogFilter(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    AuditEntityType? EntityType = null,
    AuditAction? Action = null,
    int MaxEntries = 500);

public record AuditEntryDto(
    int AuditEntryId,
    DateTime TimeStampUtc,
    AuditAction Action,
    AuditEntityType EntityType,
    int? EntityId,
    string PayloadJson);

/// <param name="Entries">Newest first (descending AuditEntryId), at most MaxEntries.</param>
/// <param name="TotalCount">Number of entries that match the filter, before the limit.</param>
public record AuditLogPage(IReadOnlyList<AuditEntryDto> Entries, int TotalCount);

/// <summary>Read-only access to the audit log. There is deliberately no update or delete.</summary>
public interface IAuditLogService
{
    Task<AuditLogPage> GetEntriesAsync(AuditLogFilter filter);
}
