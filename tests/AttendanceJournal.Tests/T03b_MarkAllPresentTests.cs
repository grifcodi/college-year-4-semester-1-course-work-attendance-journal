using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T03b_MarkAllPresentTests : AttendanceTestBase
{
    [Fact]
    public async Task MarksOnlyUnmarkedActiveStudents_AsPresent()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var lesson = await CreateLessonAsync(group, subject);
        var alreadyAbsent = await AddStudentAsync(group, "А Відсутній");
        var unmarked = await AddStudentAsync(group, "Б Без відмітки");
        var archived = await AddStudentAsync(group, "В Архівний");
        await MarkAsync(lesson, alreadyAbsent, AttendanceStatus.Absent);
        await Students.ArchiveAsync(archived);

        var result = await Attendance.MarkAllPresentAsync(lesson);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);

        var sheet = await Attendance.GetLessonSheetAsync(lesson);
        Assert.Equal(AttendanceStatus.Absent, sheet.Single(r => r.StudentId == alreadyAbsent).CurrentMark!.Status);
        Assert.Equal(AttendanceStatus.Present, sheet.Single(r => r.StudentId == unmarked).CurrentMark!.Status);
        Assert.DoesNotContain(sheet, r => r.StudentId == archived);
        Assert.Empty(await Attendance.GetHistoryAsync(lesson, archived));
    }

    [Fact]
    public async Task ThirtyStudents_AreMarkedInOneAction_WithOneAuditEntry()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var lesson = await CreateLessonAsync(group, subject);
        for (var i = 1; i <= 30; i++)
            await AddStudentAsync(group, $"Студент {i:00}");

        var result = await Attendance.MarkAllPresentAsync(lesson);

        Assert.Equal(30, result.Value);
        Assert.Equal(30, await CountRecordsAsync());
        Assert.Equal(30, await CountVersionsAsync());
        var entry = Assert.Single(await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, lesson),
            e => e.Action == AuditAction.MarkAttendance);
        Assert.Contains("30", entry.PayloadJson);
    }

    [Fact]
    public async Task MarkedStudents_GetVersion1_WithoutReason()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        await Attendance.MarkAllPresentAsync(lessonId);

        var version = Assert.Single(await Attendance.GetHistoryAsync(lessonId, studentId));
        Assert.Equal(1, version.VersionNumber);
        Assert.Null(version.Reason);
        Assert.Null(version.Grade);
    }

    [Fact]
    public async Task SecondCall_MarksNobody_AndWritesNoNewAuditEntry()
    {
        var (_, _, _, lessonId) = await SeedOneStudentLessonAsync();
        await Attendance.MarkAllPresentAsync(lessonId);
        var auditBefore = (await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, lessonId)).Count;

        var again = await Attendance.MarkAllPresentAsync(lessonId);

        Assert.True(again.IsSuccess);
        Assert.Equal(0, again.Value);
        Assert.Equal(1, await CountVersionsAsync());
        Assert.Equal(auditBefore, (await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, lessonId)).Count);
    }

    [Fact]
    public async Task LessonWithoutStudents_ReturnsZero()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var lesson = await CreateLessonAsync(group, subject);

        var result = await Attendance.MarkAllPresentAsync(lesson);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task UnknownLesson_IsRejected()
    {
        var result = await Attendance.MarkAllPresentAsync(999);

        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }
}