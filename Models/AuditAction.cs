using System;
using System.Collections.Generic;
using System.Text;

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