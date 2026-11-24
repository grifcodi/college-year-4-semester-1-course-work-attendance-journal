using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class MonthGridService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), IMonthGridService
{
    private sealed record LatestMark(int LessonId, int StudentId, AttendanceStatus Status, int? Grade);

    public async Task<MonthGridDto> GetMonthGridAsync(int groupId, int? subjectId, int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9998)
            return new MonthGridDto(groupId, subjectId, year, month, [], []);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var lessonsQuery = context.Lessons
            .AsNoTracking()
            .Where(l => l.StudentGroupId == groupId && l.Date >= startDate && l.Date <= endDate);

        if (subjectId is int sid)
            lessonsQuery = lessonsQuery.Where(l => l.SubjectId == sid);

        var lessons = await lessonsQuery
            .OrderBy(l => l.Date)
            .ThenBy(l => l.PairNumber)
            .Select(l => new MonthGridLessonDto(
                l.LessonId,
                l.Date,
                l.PairNumber,
                l.SubjectId,
                l.Subject.Name,
                l.Topic))
            .ToListAsync();

        var students = await context.Students
            .AsNoTracking()
            .Where(s => s.StudentGroupId == groupId)
            .ToListAsync();

        var marks = new List<LatestMark>();
        if (lessons.Count > 0)
        {
            var lessonIds = lessons.Select(l => l.LessonId).ToList();

            marks = await context.AttendanceVersions
                .AsNoTracking()
                .Where(v => lessonIds.Contains(v.AttendanceRecord.LessonId))
                .Where(v => v.VersionNumber == context.AttendanceVersions
                    .Where(inner => inner.AttendanceRecordId == v.AttendanceRecordId)
                    .Max(inner => inner.VersionNumber))
                .Select(v => new LatestMark(
                    v.AttendanceRecord.LessonId,
                    v.AttendanceRecord.StudentId,
                    v.Status,
                    v.Grade))
                .ToListAsync();
        }

        var lessonIndex = lessons
            .Select((l, i) => (l.LessonId, i))
            .ToDictionary(x => x.LessonId, x => x.i);

        var cellsByStudent = new Dictionary<int, MonthGridCellDto?[]>();
        foreach (var mark in marks)
        {
            if (!cellsByStudent.TryGetValue(mark.StudentId, out var cells))
            {
                cells = new MonthGridCellDto?[lessons.Count];
                cellsByStudent[mark.StudentId] = cells;
            }

            cells[lessonIndex[mark.LessonId]] = new MonthGridCellDto(mark.Status, mark.Grade);
        }

        var rows = students
            .Where(s => !s.IsArchived || cellsByStudent.ContainsKey(s.StudentId))
            .OrderByName(s => s.FullName)
            .Select(s => new MonthGridRowDto(
                s.StudentId,
                s.FullName,
                s.IsArchived,
                cellsByStudent.GetValueOrDefault(s.StudentId) ?? new MonthGridCellDto?[lessons.Count]))
            .ToList();

        return new MonthGridDto(groupId, subjectId, year, month, lessons, rows);
    }
}
