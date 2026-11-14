using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

internal class Student
{
    public int StudentId { get; set; }
    public int StudentGroupId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public StudentGroup StudentGroup { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
