using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

internal class Lesson
{
    public int LessonId { get; set; }
    public int StudentGroupId { get; set; }
    public int SubjectId { get; set; }
    public DateOnly Date { get; set; }
    public int PairNumber { get; set; }
    public string? Topic { get; set; }

    public StudentGroup StudentGroup { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
