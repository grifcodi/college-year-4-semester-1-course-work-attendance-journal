using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public interface ILessonService
{
    Task<List<Lesson>> GetByMonthAsync(int groupId, int subjectId, int year, int month);

    Task<Lesson?> GetByIdAsync(int lessonId);

    Task<Result<Lesson>> CreateAsync(
        int groupId,
        int subjectId,
        DateOnly date,
        int pairNumber,
        string? topic);

    Task<Result<Lesson>> UpdateTopicAsync(int lessonId, string? topic);
}
