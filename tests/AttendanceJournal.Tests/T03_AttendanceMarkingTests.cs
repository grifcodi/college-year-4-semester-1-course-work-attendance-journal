using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T03_AttendanceMarkingTests : AttendanceTestBase
{
    [Fact]
    public async Task FirstMark_CreatesVersion1_WithoutReason()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Late, reason: "ця причина ігнорується");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.VersionNumber);
        var version = Assert.Single(await Attendance.GetHistoryAsync(lessonId, studentId));
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(AttendanceStatus.Late, version.Status);
        Assert.Null(version.Reason);
    }

    [Theory]
    [InlineData(AttendanceStatus.Present)]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Late)]
    [InlineData(AttendanceStatus.Excused)]
    public async Task EveryStatus_CanBeSaved(AttendanceStatus status)
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, status);

        Assert.True(result.IsSuccess);
        Assert.Equal(status, result.Value!.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(null)]
    public async Task Grade_BoundariesAndNull_AreAccepted(int? grade)
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, grade: grade);

        Assert.True(result.IsSuccess);
        Assert.Equal(grade, result.Value!.Grade);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task Grade_OutOfRange_IsRejected_AndNothingIsSaved(int grade)
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, grade: grade);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.AttendanceInvalidGrade, result.ErrorKey);
        Assert.Equal(0, await CountRecordsAsync());
        Assert.Equal(0, await CountVersionsAsync());
    }

    [Fact]
    public async Task Grade_IsAllowedForAbsentStudent()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, grade: 75);

        Assert.True(result.IsSuccess);
        Assert.Equal(AttendanceStatus.Absent, result.Value!.Status);
        Assert.Equal(75, result.Value.Grade);
    }

    [Fact]
    public async Task Note_IsTrimmed_AndBlankBecomesNull()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var s1 = await AddStudentAsync(group, "Бойко Олена");
        var s2 = await AddStudentAsync(group, "Франко Іван");
        var lesson = await CreateLessonAsync(group, subject);

        var withNote = await MarkAsync(lesson, s1, note: "  запізнився на 10 хв  ");
        var blank = await MarkAsync(lesson, s2, note: "   ");

        Assert.Equal("запізнився на 10 хв", withNote.Value!.Note);
        Assert.Null(blank.Value!.Note);
    }

    [Fact]
    public async Task Note_Limit_AcceptsMaxAndRejectsOneMore()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var s1 = await AddStudentAsync(group, "Бойко Олена");
        var s2 = await AddStudentAsync(group, "Франко Іван");
        var lesson = await CreateLessonAsync(group, subject);
        var max = ValidationConstants.NoteMaxLength;

        var ok = await MarkAsync(lesson, s1, note: new string('а', max));
        var tooLong = await MarkAsync(lesson, s2, note: new string('б', max + 1));

        Assert.True(ok.IsSuccess);
        Assert.Equal(ErrorKeys.AttendanceNoteTooLong, tooLong.ErrorKey);
    }

    [Fact]
    public async Task UnknownLessonOrStudent_IsRejected()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var noLesson = await MarkAsync(999, studentId);
        var noStudent = await MarkAsync(lessonId, 999);

        Assert.Equal(ErrorKeys.EntityNotFound, noLesson.ErrorKey);
        Assert.Equal(ErrorKeys.EntityNotFound, noStudent.ErrorKey);
        Assert.Equal(0, await CountRecordsAsync());
    }

    [Fact]
    public async Task StudentFromAnotherGroup_IsRejected()
    {
        var (_, _, _, lessonId) = await SeedOneStudentLessonAsync();
        var otherGroup = await CreateGroupAsync("ІПЗ-22");
        var outsider = await AddStudentAsync(otherGroup, "Сторонній Студент");

        var result = await MarkAsync(lessonId, outsider);

        Assert.Equal(ErrorKeys.AttendanceStudentWrongGroup, result.ErrorKey);
        Assert.Equal(0, await CountRecordsAsync());
    }

    [Fact]
    public async Task ArchivedStudent_WithoutMark_CannotBeMarked()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await Students.ArchiveAsync(studentId);

        var result = await MarkAsync(lessonId, studentId);

        Assert.Equal(ErrorKeys.AttendanceStudentArchived, result.ErrorKey);
        Assert.Equal(0, await CountRecordsAsync());
    }

    [Fact]
    public async Task ArchivedStudent_WithExistingMark_CanStillBeEdited()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await MarkAsync(lessonId, studentId, AttendanceStatus.Present);
        await Students.ArchiveAsync(studentId);

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "виправлення помилки");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.VersionNumber);
    }

    [Fact]
    public async Task SecondMark_ReusesTheSameRecord()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var first = await MarkAsync(lessonId, studentId, AttendanceStatus.Present);
        var second = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "помилився при введенні");

        Assert.Equal(first.Value!.AttendanceRecordId, second.Value!.AttendanceRecordId);
        Assert.Equal(1, await CountRecordsAsync());
        Assert.Equal(2, await CountVersionsAsync());
    }

    [Fact]
    public async Task Mark_WritesOneMarkAttendanceAuditEntry_WithStatusGradeAndNote()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Late, grade: 88, note: "пізно");

        var entry = Assert.Single(await ContextFactory.GetAuditAsync(
            AuditEntityType.AttendanceRecord, result.Value!.AttendanceRecordId));
        Assert.Equal(AuditAction.MarkAttendance, entry.Action);
        Assert.Contains("Late", entry.PayloadJson);
        Assert.Contains("88", entry.PayloadJson);
        Assert.Contains("пізно", entry.PayloadJson);
    }

    [Fact]
    public async Task FailedMark_WritesNoAuditEntry()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        await MarkAsync(lessonId, studentId, grade: 150);
        await MarkAsync(999, studentId);

        Assert.Empty(await ContextFactory.GetAuditAsync(AuditEntityType.AttendanceRecord));
    }

    [Fact]
    public async Task ThirtyStudents_CanBeMarkedOneByOne_IntoDistinctRecords()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var lesson = await CreateLessonAsync(group, subject);
        var ids = new List<int>();
        for (var i = 1; i <= 30; i++)
            ids.Add(await AddStudentAsync(group, $"Студент {i:00}"));

        foreach (var id in ids)
            Assert.True((await MarkAsync(lesson, id)).IsSuccess);

        Assert.Equal(30, await CountRecordsAsync());
        Assert.Equal(30, await CountVersionsAsync());
    }
}