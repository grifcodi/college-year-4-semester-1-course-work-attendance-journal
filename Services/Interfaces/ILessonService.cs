using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

/// <summary>
/// A repeating lesson: the same group, subject and pair on one day of the week,
/// every <see cref="WeekInterval"/> weeks between two dates (both inclusive).
/// </summary>
public record LessonSeriesRequest(
    int GroupId,
    int SubjectId,
    DayOfWeek DayOfWeek,
    int PairNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekInterval = 1,
    string? Topic = null);

/// <param name="NewDates">Dates where a lesson will be created.</param>
/// <param name="TakenDates">Dates skipped because the group already has a lesson at that pair.</param>
public record LessonSeriesPlan(IReadOnlyList<DateOnly> NewDates, IReadOnlyList<DateOnly> TakenDates);

public record LessonSeriesResult(IReadOnlyList<int> CreatedLessonIds, IReadOnlyList<DateOnly> SkippedDates);

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

    /// <summary>Lists the dates of a repeating lesson without changing the database.</summary>
    Task<Result<LessonSeriesPlan>> PlanSeriesAsync(LessonSeriesRequest request);

    /// <summary>
    /// Creates all lessons of the series in one transaction and writes one CreateSeries audit entry.
    /// Dates where the pair is already taken are skipped and reported, not treated as errors.
    /// </summary>
    Task<Result<LessonSeriesResult>> CreateSeriesAsync(LessonSeriesRequest request);
}
