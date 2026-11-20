using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T04_AttendanceEditTests : AttendanceTestBase
{
    private async Task<(int StudentId, int LessonId)> SeedMarkedAsync(
        AttendanceStatus status = AttendanceStatus.Present, int? grade = null, string? note = null)
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        var first = await MarkAsync(lessonId, studentId, status, grade, note);
        Assert.True(first.IsSuccess);
        return (studentId, lessonId);
    }

    [Fact]
    public async Task Edit_CreatesVersion2_AndKeepsVersion1Readable()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Present, grade: 90);

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, grade: 90, reason: "помилка введення");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.VersionNumber);

        var history = await Attendance.GetHistoryAsync(lessonId, studentId);
        Assert.Equal([2, 1], history.Select(v => v.VersionNumber));
        Assert.Equal(AttendanceStatus.Present, history[1].Status);
        Assert.Null(history[1].Reason);
        Assert.Equal(AttendanceStatus.Absent, history[0].Status);
        Assert.Equal("помилка введення", history[0].Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    public async Task Edit_WithoutReason_IsBlocked_AndNothingChanges(string? reason)
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: reason);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.AttendanceReasonRequired, result.ErrorKey);
        Assert.Equal(1, await CountVersionsAsync());
        Assert.Single(await ContextFactory.GetAuditAsync(AuditEntityType.AttendanceRecord));
    }

    [Fact]
    public async Task Edit_ReasonShorterThanFiveCharacters_IsBlocked()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "abcd");

        Assert.Equal(ErrorKeys.AttendanceReasonTooShort, result.ErrorKey);
        Assert.Equal([ValidationConstants.ReasonMinLength], result.ErrorArgs);
        Assert.Equal(1, await CountVersionsAsync());
    }

    [Fact]
    public async Task Edit_ReasonOfExactlyFiveCharacters_IsAccepted()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "abcde");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Edit_ReasonLength_IsMeasuredAfterTrimming()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "  ab  ");

        Assert.Equal(ErrorKeys.AttendanceReasonTooShort, result.ErrorKey);
    }

    [Fact]
    public async Task Edit_ReasonIsStoredTrimmed_AndTooLongIsRejected()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var tooLong = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent,
            reason: new string('а', ValidationConstants.ReasonMaxLength + 1));
        var ok = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "  виправлення  ");

        Assert.Equal(ErrorKeys.AttendanceReasonTooLong, tooLong.ErrorKey);
        Assert.True(ok.IsSuccess);
        Assert.Equal("виправлення", (await Attendance.GetHistoryAsync(lessonId, studentId))[0].Reason);
    }

    [Fact]
    public async Task Edit_WhenNothingChanged_IsRejected_EvenWithValidReason()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Late, grade: 70, note: "пізно");

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Late, grade: 70, note: "пізно",
            reason: "добра причина");

        Assert.Equal(ErrorKeys.AttendanceNoChange, result.ErrorKey);
        Assert.Equal(1, await CountVersionsAsync());
    }

    [Fact]
    public async Task Edit_WhenNothingChanged_ReportsNoChange_BeforeAskingForReason()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Present, reason: null);

        Assert.Equal(ErrorKeys.AttendanceNoChange, result.ErrorKey);
    }

    [Fact]
    public async Task Edit_OnlyGradeChanged_IsAccepted()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Present, grade: 70);

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Present, grade: 80, reason: "перерахунок");

        Assert.True(result.IsSuccess);
        Assert.Equal(80, result.Value!.Grade);
    }

    [Fact]
    public async Task Edit_OnlyNoteChanged_IsAccepted()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Late, note: "пізно");

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Late, note: "пізно на 5 хв", reason: "уточнення");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Edit_GradeClearedToNull_CountsAsAChange()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Present, grade: 70);

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Present, grade: null, reason: "оцінку скасовано");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Grade);
    }

    [Fact]
    public async Task Edit_BackToEarlierValues_IsAllowed_AsNewVersion()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Present);
        await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "помилково");

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Present, reason: "повернуто назад");

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.VersionNumber);
        Assert.Equal(3, (await Attendance.GetHistoryAsync(lessonId, studentId)).Count);
    }

    [Fact]
    public async Task Edit_WritesSecondAuditEntry_WithVersionAndReason()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();

        var result = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "виправлення журналу");

        var entries = await ContextFactory.GetAuditAsync(AuditEntityType.AttendanceRecord, result.Value!.AttendanceRecordId);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal(AuditAction.MarkAttendance, e.Action));
        Assert.Contains("виправлення журналу", entries[1].PayloadJson);
        Assert.DoesNotContain("виправлення журналу", entries[0].PayloadJson);
    }

    [Fact]
    public async Task EditOfOneStudent_DoesNotAffectAnotherStudentsMark()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var s1 = await AddStudentAsync(group, "Бойко Олена");
        var s2 = await AddStudentAsync(group, "Франко Іван");
        var lesson = await CreateLessonAsync(group, subject);
        await MarkAsync(lesson, s1);
        await MarkAsync(lesson, s2);

        await MarkAsync(lesson, s1, AttendanceStatus.Absent, reason: "виправлення");

        Assert.Single(await Attendance.GetHistoryAsync(lesson, s2));
        Assert.Equal(2, (await Attendance.GetHistoryAsync(lesson, s1)).Count);
    }


    [Fact]
    public async Task Restore_CreatesNewVersion_WithOldValues_AndKeepsHistory()
    {
        var (studentId, lessonId) = await SeedMarkedAsync(AttendanceStatus.Present, grade: 90, note: "вчасно");
        var second = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, grade: 0, note: "відсутній", reason: "помилка");
        var recordId = second.Value!.AttendanceRecordId;

        var restored = await Attendance.RestoreVersionAsync(recordId, 1, "Restore to version 1");

        Assert.True(restored.IsSuccess);
        Assert.Equal(3, restored.Value!.VersionNumber);
        Assert.Equal(AttendanceStatus.Present, restored.Value.Status);
        Assert.Equal(90, restored.Value.Grade);
        Assert.Equal("вчасно", restored.Value.Note);
        var history = await Attendance.GetHistoryAsync(lessonId, studentId);
        Assert.Equal([3, 2, 1], history.Select(v => v.VersionNumber));
        Assert.Equal("Restore to version 1", history[0].Reason);
    }

    [Fact]
    public async Task Restore_ToTheCurrentVersion_IsRejectedAsNoChange()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();
        var recordId = (await Attendance.GetHistoryAsync(lessonId, studentId))[0].AttendanceRecordId;

        var result = await Attendance.RestoreVersionAsync(recordId, 1, "Restore to version 1");

        Assert.Equal(ErrorKeys.AttendanceNoChange, result.ErrorKey);
        Assert.Equal(1, await CountVersionsAsync());
    }

    [Fact]
    public async Task Restore_Validation_ReasonMissingShortUnknownVersionUnknownRecord()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();
        var second = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "помилка");
        var recordId = second.Value!.AttendanceRecordId;

        Assert.Equal(ErrorKeys.AttendanceReasonRequired, (await Attendance.RestoreVersionAsync(recordId, 1, "  ")).ErrorKey);
        Assert.Equal(ErrorKeys.AttendanceReasonTooShort, (await Attendance.RestoreVersionAsync(recordId, 1, "abc")).ErrorKey);
        Assert.Equal(ErrorKeys.AttendanceVersionNotFound, (await Attendance.RestoreVersionAsync(recordId, 9, "valid reason")).ErrorKey);
        Assert.Equal(ErrorKeys.EntityNotFound, (await Attendance.RestoreVersionAsync(999, 1, "valid reason")).ErrorKey);
        Assert.Equal(2, await CountVersionsAsync());
    }

    [Fact]
    public async Task Restore_WritesRestoreVersionAuditEntry()
    {
        var (studentId, lessonId) = await SeedMarkedAsync();
        var second = await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "помилка");
        var recordId = second.Value!.AttendanceRecordId;

        await Attendance.RestoreVersionAsync(recordId, 1, "Restore to version 1");

        var entries = await ContextFactory.GetAuditAsync(AuditEntityType.AttendanceRecord, recordId);
        Assert.Equal(AuditAction.RestoreVersion, entries.Last().Action);
        Assert.Contains("Restore to version 1", entries.Last().PayloadJson);
    }
}