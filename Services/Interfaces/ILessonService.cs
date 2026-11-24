using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public interface ILessonService
{
    /// <summary>Lessons of a group in a month; <paramref name="subjectId"/> null means all subjects (D9).</summary>
    Task<List<Lesson>> GetByMonthAsync(int groupId, int? subjectId, int year, int month);

    /// <summary>Lessons of a group between two dates (inclusive), ordered by date and pair, with Subject loaded.</summary>
    Task<List<Lesson>> GetByRangeAsync(int groupId, DateOnly fromDate, DateOnly toDate, int? subjectId = null);

    Task<Lesson?> GetByIdAsync(int lessonId);

    Task<Result<Lesson>> CreateAsync(
        int groupId,
        int subjectId,
        DateOnly date,
        int pairNumber,
        string? topic);

    Task<Result<Lesson>> UpdateTopicAsync(int lessonId, string? topic);
}
