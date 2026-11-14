using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

internal class StudentGroup
{
    public int StudentGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public ICollection<Student> Students { get; set; } = [];
    public ICollection<Lesson> Lessons { get; set; } = [];
}
