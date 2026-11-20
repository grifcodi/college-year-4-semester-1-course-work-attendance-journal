using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests.TestHelpers;

public abstract class AttendanceTestBase : SqliteInMemoryTestBase
{
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected StudentGroupService Groups => new(ContextFactory);
    protected SubjectService Subjects => new(ContextFactory);
    protected StudentService Students => new(ContextFactory);
    protected LessonService Lessons => new(ContextFactory);
    protected AttendanceService Attendance => new(ContextFactory);

    protected async Task<int> CreateGroupAsync(string name = "ІПЗ-21") =>
        (await Groups.CreateAsync(name, "2026-2027")).Value!.StudentGroupId;

    protected async Task<int> CreateSubjectAsync(string name = "Математика") =>
        (await Subjects.CreateAsync(name)).Value!.SubjectId;

    protected async Task<int> AddStudentAsync(int groupId, string name = "Шевченко Тарас") =>
        (await Students.AddAsync(groupId, name)).Value!.StudentId;

    protected async Task<int> CreateLessonAsync(int groupId, int subjectId, int day = 10, int pair = 1) =>
        (await Lessons.CreateAsync(groupId, subjectId, new DateOnly(2026, 11, day), pair, null)).Value!.LessonId;
    protected async Task<(int GroupId, int SubjectId, int StudentId, int LessonId)> SeedOneStudentLessonAsync()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var studentId = await AddStudentAsync(groupId);
        var lessonId = await CreateLessonAsync(groupId, subjectId);
        return (groupId, subjectId, studentId, lessonId);
    }

    protected Task<Result<AttendanceCellDto>> MarkAsync(
        int lessonId,
        int studentId,
        AttendanceStatus status = AttendanceStatus.Present,
        int? grade = null,
        string? note = null,
        string? reason = null) =>
        Attendance.SaveMarkAsync(lessonId, studentId, status, grade, note, reason);

    protected async Task<int> CountVersionsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.AttendanceVersions.CountAsync(Ct);
    }

    protected async Task<int> CountRecordsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.AttendanceRecords.CountAsync(Ct);
    }
}