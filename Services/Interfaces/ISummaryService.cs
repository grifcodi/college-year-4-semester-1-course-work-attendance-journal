namespace AttendanceJournal.Services.Interfaces;

public record StudentSummaryDto(
    int StudentId,
    string FullName,
    bool IsArchived,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int ExcusedCount,
    int UnmarkedCount,
    double? AttendanceRatePercent,
    int GradedLessonsCount,
    double? AverageGrade);

public record GroupSummaryDto(
    int GroupId,
    int? SubjectId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalLessonsCount,
    IReadOnlyList<StudentSummaryDto> Students,
    double? GroupAttendanceRatePercent,
    double? GroupAverageGrade,
    int QualifyingStudentsCount);

public interface ISummaryService
{
    Task<GroupSummaryDto> GetGroupSummaryAsync(
        int groupId,
        int? subjectId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool includeArchivedStudents = false);
}