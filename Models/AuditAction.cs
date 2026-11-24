namespace AttendanceJournal.Models;

public enum AuditAction
{
    Create,
    Update,
    Archive,
    Unarchive,
    MarkAttendance,
    BulkImport,
    RestoreVersion,
    /// <summary>A repeating lesson: many lessons created in one action.</summary>
    CreateSeries,
}
