using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

internal enum AuditEntityType
{
    StudentGroup,
    Student,
    Subject,
    Lesson,
    AttendanceRecord,
    Setting,
}