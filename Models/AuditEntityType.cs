using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

public enum AuditEntityType
{
    StudentGroup,
    Student,
    Subject,
    Lesson,
    AttendanceRecord,
    Setting,
}