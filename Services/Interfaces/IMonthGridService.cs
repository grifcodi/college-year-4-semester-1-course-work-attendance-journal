using AttendanceJournal.Models;

namespace AttendanceJournal.Services.Interfaces;

public record MonthGridLessonDto(
    int LessonId,
    DateOnly Date,
    int PairNumber,
    int SubjectId,
    string SubjectName,
    string? Topic);

/// <param name="Cells">Current mark per lesson, in the order of <see cref="MonthGridDto.Lessons"/>; null = not marked.</param>
public record MonthGridCellDto(AttendanceStatus Status, int? Grade);

public record MonthGridRowDto(
    int StudentId,
    string FullName,
    bool IsArchived,
    IReadOnlyList<MonthGridCellDto?> Cells);

public record MonthGridDto(
    int GroupId,
    int? SubjectId,
    int Year,
    int Month,
    IReadOnlyList<MonthGridLessonDto> Lessons,
    IReadOnlyList<MonthGridRowDto> Rows);

/// <summary>Data for the month grid (FR-08, NFR-01).</summary>
public interface IMonthGridService
{
    /// <summary>
    /// Rows are the active students of the group plus archived students who have marks in the month.
    /// <paramref name="subjectId"/> null shows all subjects (D9).
    /// </summary>
    Task<MonthGridDto> GetMonthGridAsync(int groupId, int? subjectId, int year, int month);
}
