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
}