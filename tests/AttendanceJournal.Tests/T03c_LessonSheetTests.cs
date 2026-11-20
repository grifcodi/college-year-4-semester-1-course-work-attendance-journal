using AttendanceJournal.Models;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T03c_LessonSheetTests : AttendanceTestBase
{
    [Fact]
    public async Task Sheet_ListsActiveStudentsByName_WithNullMarkWhenNotMarked()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var lesson = await CreateLessonAsync(group, subject);
        await AddStudentAsync(group, "Франко Іван");
        await AddStudentAsync(group, "Бойко Олена");

        var sheet = await Attendance.GetLessonSheetAsync(lesson);

        Assert.Equal(["Бойко Олена", "Франко Іван"], sheet.Select(r => r.FullName));
        Assert.All(sheet, r => Assert.Null(r.CurrentMark));
    }

    [Fact]
    public async Task Sheet_ShowsLatestVersion_AsCurrentMark()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await MarkAsync(lessonId, studentId, AttendanceStatus.Present);
        await MarkAsync(lessonId, studentId, AttendanceStatus.Late, grade: 60, reason: "виправлення");

        var row = Assert.Single(await Attendance.GetLessonSheetAsync(lessonId));

        Assert.Equal(2, row.CurrentMark!.VersionNumber);
        Assert.Equal(AttendanceStatus.Late, row.CurrentMark.Status);
        Assert.Equal(60, row.CurrentMark.Grade);
    }

    [Fact]
    public async Task Sheet_HidesArchivedStudentWithoutMark_ButKeepsOneWithHistory()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var withMark = await AddStudentAsync(group, "А З відміткою");
        var withoutMark = await AddStudentAsync(group, "Б Без відмітки");
        var lesson = await CreateLessonAsync(group, subject);
        await MarkAsync(lesson, withMark, AttendanceStatus.Present);
        await Students.ArchiveAsync(withMark);
        await Students.ArchiveAsync(withoutMark);

        var sheet = await Attendance.GetLessonSheetAsync(lesson);

        var row = Assert.Single(sheet);
        Assert.Equal(withMark, row.StudentId);
        Assert.NotNull(row.CurrentMark);
    }

    [Fact]
    public async Task Sheet_ForUnknownLesson_IsEmpty()
    {
        Assert.Empty(await Attendance.GetLessonSheetAsync(999));
    }

    [Fact]
    public async Task GetLatest_ReturnsOneCurrentMarkPerRecord_AcrossLessons()
    {
        var group = await CreateGroupAsync();
        var subject = await CreateSubjectAsync();
        var s1 = await AddStudentAsync(group, "Бойко Олена");
        var s2 = await AddStudentAsync(group, "Франко Іван");
        var lesson1 = await CreateLessonAsync(group, subject, day: 10);
        var lesson2 = await CreateLessonAsync(group, subject, day: 11);
        await MarkAsync(lesson1, s1, AttendanceStatus.Present);
        await MarkAsync(lesson1, s1, AttendanceStatus.Absent, reason: "виправлення");
        await MarkAsync(lesson1, s2, AttendanceStatus.Late);
        await MarkAsync(lesson2, s1, AttendanceStatus.Excused);

        var latest = await Attendance.GetLatestForLessonsAsync([lesson1, lesson2]);

        Assert.Equal(3, latest.Count);
        var s1Lesson1 = latest.Single(m => m.LessonId == lesson1 && m.StudentId == s1);
        Assert.Equal(2, s1Lesson1.VersionNumber);
        Assert.Equal(AttendanceStatus.Absent, s1Lesson1.Status);
    }

    [Fact]
    public async Task GetLatest_ForNoLessons_ReturnsEmpty()
    {
        Assert.Empty(await Attendance.GetLatestForLessonsAsync([]));
    }

    [Fact]
    public async Task GetLatest_IgnoresLessonsNotRequested()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await MarkAsync(lessonId, studentId);

        Assert.Empty(await Attendance.GetLatestForLessonsAsync([999]));
    }

    [Fact]
    public async Task History_IsNewestFirst_AndEmptyForUnmarkedStudent()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        Assert.Empty(await Attendance.GetHistoryAsync(lessonId, studentId));

        await MarkAsync(lessonId, studentId, AttendanceStatus.Present);
        await MarkAsync(lessonId, studentId, AttendanceStatus.Absent, reason: "виправлення");

        var history = await Attendance.GetHistoryAsync(lessonId, studentId);
        Assert.Equal([2, 1], history.Select(v => v.VersionNumber));
    }
}