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

    Task<Result<int>> MarkAllPresentAsync(int lessonId);

    Task<Result<AttendanceCellDto>> RestoreVersionAsync(
        int attendanceRecordId,
        int targetVersionNumber,
        string reason);
}