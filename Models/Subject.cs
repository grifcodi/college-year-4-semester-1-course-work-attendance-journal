namespace AttendanceJournal.Models;

public class Subject
{
    public int SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = [];
}
