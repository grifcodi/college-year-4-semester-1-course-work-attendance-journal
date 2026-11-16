using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

public class AttendanceRecord
{
    public int AttendanceRecordId { get; set; }
    public int LessonId { get; set; }
    public int StudentId { get; set; }

    public Lesson Lesson { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<AttendanceVersion> Versions { get; set; } = [];
}
