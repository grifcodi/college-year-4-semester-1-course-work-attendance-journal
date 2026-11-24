using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public record AttendanceCellDto(
    int AttendanceRecordId,
    int LessonId,
    int StudentId,
    int VersionNumber,
    AttendanceStatus Status,
    int? Grade,
    string? Note);

public record LessonStudentRowDto(
    int StudentId,
    string FullName,
    AttendanceCellDto? CurrentMark);

/// <summary>One student's mark in a batch save. Grade is 0–100 or null.</summary>
public record MarkInput(
    int StudentId,
    AttendanceStatus Status,
    int? Grade,
    string? Note);

public interface IAttendanceService
{
    Task<List<LessonStudentRowDto>> GetLessonSheetAsync(int lessonId);

    Task<List<AttendanceCellDto>> GetLatestForLessonsAsync(IEnumerable<int> lessonIds);

    Task<List<AttendanceVersion>> GetHistoryAsync(int lessonId, int studentId);

    Task<Result<AttendanceCellDto>> SaveMarkAsync(
        int lessonId,
        int studentId,
        AttendanceStatus status,
        int? grade,
        string? note,
        string? reason);

    /// <summary>
    /// Saves several marks of one lesson in a single transaction (FR-05, NFR-02).
    /// The reason is required only for marks that already have a saved version (FR-07).
    /// </summary>
    Task<Result<List<AttendanceCellDto>>> SaveMarksAsync(
        int lessonId,
        IReadOnlyList<MarkInput> marks,
        string? reason);

    Task<Result<int>> MarkAllPresentAsync(int lessonId);

    Task<Result<AttendanceCellDto>> RestoreVersionAsync(
        int attendanceRecordId,
        int targetVersionNumber,
        string reason);
}
