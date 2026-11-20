using AttendanceJournal.Models;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T20_DatabaseConstraintTests : AttendanceTestBase
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Database_RejectsGradeOutsideRange(int grade)
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        var record = new AttendanceRecord { LessonId = lessonId, StudentId = studentId };
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync(Ct);
        context.AttendanceVersions.Add(new AttendanceVersion
        {
            AttendanceRecordId = record.AttendanceRecordId,
            VersionNumber = 1,
            Status = AttendanceStatus.Present,
            Grade = grade,
            TimeStamp = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task Database_RejectsPairNumberOutsideRange(int pair)
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        context.Lessons.Add(new Lesson
        {
            StudentGroupId = groupId,
            SubjectId = subjectId,
            Date = new DateOnly(2026, 11, 10),
            PairNumber = pair
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Database_RejectsDuplicateVersionNumberForOneRecord()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        var first = await MarkAsync(lessonId, studentId);
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        context.AttendanceVersions.Add(new AttendanceVersion
        {
            AttendanceRecordId = first.Value!.AttendanceRecordId,
            VersionNumber = 1,
            Status = AttendanceStatus.Absent,
            TimeStamp = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Database_RejectsSecondRecordForSameLessonAndStudent()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();
        await MarkAsync(lessonId, studentId);
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        context.AttendanceRecords.Add(new AttendanceRecord { LessonId = lessonId, StudentId = studentId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }
}