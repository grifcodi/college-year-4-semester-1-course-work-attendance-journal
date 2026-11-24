using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class SummaryService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), ISummaryService
{
    public const int MinimumGradesForAverage = 3;

    private sealed record LatestMark(int StudentId, AttendanceStatus Status, int? Grade);

    /// <param name="includeArchivedStudents">
    /// false (default): archived students are listed only if they have marks in the range,
    /// so their history still counts toward the group rate (spec 6.3).
    /// true: every archived student is listed, even without marks.
    /// </param>
    public async Task<GroupSummaryDto> GetGroupSummaryAsync(
        int groupId,
        int? subjectId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool includeArchivedStudents = false)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var allStudents = await context.Students
            .AsNoTracking()
            .Where(s => s.StudentGroupId == groupId)
            .ToListAsync();

        var lessonsQuery = context.Lessons
            .AsNoTracking()
            .Where(l => l.StudentGroupId == groupId);

        if (subjectId is int sid)
            lessonsQuery = lessonsQuery.Where(l => l.SubjectId == sid);

        if (fromDate is DateOnly from)
            lessonsQuery = lessonsQuery.Where(l => l.Date >= from);

        if (toDate is DateOnly to)
            lessonsQuery = lessonsQuery.Where(l => l.Date <= to);

        var lessonIds = await lessonsQuery
            .Select(l => l.LessonId)
            .ToListAsync();

        var marksByStudent = new Dictionary<int, List<LatestMark>>();

        if (allStudents.Count > 0 && lessonIds.Count > 0)
        {
            var studentIds = allStudents.Select(s => s.StudentId).ToList();

            var latestVersions = await context.AttendanceVersions
                .AsNoTracking()
                .Where(v => lessonIds.Contains(v.AttendanceRecord.LessonId)
                            && studentIds.Contains(v.AttendanceRecord.StudentId))
                .Where(v => v.VersionNumber == context.AttendanceVersions
                    .Where(inner => inner.AttendanceRecordId == v.AttendanceRecordId)
                    .Max(inner => inner.VersionNumber))
                .Select(v => new LatestMark(v.AttendanceRecord.StudentId, v.Status, v.Grade))
                .ToListAsync();

            marksByStudent = latestVersions
                .GroupBy(m => m.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        var students = allStudents
            .Where(s => !s.IsArchived || includeArchivedStudents || marksByStudent.ContainsKey(s.StudentId))
            .OrderByName(s => s.FullName)
            .ToList();

        var studentSummaries = new List<StudentSummaryDto>(students.Count);
        var qualifyingRawAverages = new List<double>();

        foreach (var student in students)
        {
            var marks = marksByStudent.GetValueOrDefault(student.StudentId) ?? [];

            var present = marks.Count(m => m.Status == AttendanceStatus.Present);
            var late = marks.Count(m => m.Status == AttendanceStatus.Late);
            var absent = marks.Count(m => m.Status == AttendanceStatus.Absent);
            var excused = marks.Count(m => m.Status == AttendanceStatus.Excused);
            var unmarked = Math.Max(0, lessonIds.Count - marks.Count);

            var attendanceRate = CalculateAttendanceRate(present, late, absent);

            var grades = marks
                .Where(m => m.Grade.HasValue)
                .Select(m => m.Grade!.Value)
                .ToList();

            double? averageGrade = null;
            if (grades.Count >= MinimumGradesForAverage)
            {
                var rawAverage = grades.Average();
                qualifyingRawAverages.Add(rawAverage);
                averageGrade = Math.Round(rawAverage, 1, MidpointRounding.AwayFromZero);
            }

            studentSummaries.Add(new StudentSummaryDto(
                student.StudentId,
                student.FullName,
                student.IsArchived,
                present,
                late,
                absent,
                excused,
                unmarked,
                attendanceRate,
                grades.Count,
                averageGrade));
        }

        var groupAttendanceRate = CalculateAttendanceRate(
            studentSummaries.Sum(s => s.PresentCount),
            studentSummaries.Sum(s => s.LateCount),
            studentSummaries.Sum(s => s.AbsentCount));

        // Mean of the students' unrounded averages, rounded once at the end.
        double? groupAverageGrade = qualifyingRawAverages.Count > 0
            ? Math.Round(qualifyingRawAverages.Average(), 1, MidpointRounding.AwayFromZero)
            : null;

        return new GroupSummaryDto(
            groupId,
            subjectId,
            fromDate,
            toDate,
            lessonIds.Count,
            studentSummaries,
            groupAttendanceRate,
            groupAverageGrade,
            qualifyingRawAverages.Count);
    }

    internal static double? CalculateAttendanceRate(int present, int late, int absent)
    {
        var denominator = present + late + absent;
        if (denominator == 0)
            return null;

        var numerator = present + late;
        return Math.Round((double)numerator / denominator * 100.0, 2, MidpointRounding.AwayFromZero);
    }
}
