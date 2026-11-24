using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Tests.TestHelpers;
using System.Diagnostics;

namespace AttendanceJournal.Tests;

/// <summary>
/// T-11 (NFR-01): the month grid data for 30 students × 120 lessons loads in under 1 second.
/// The measured part is the service call that the month grid screen makes; rendering is checked manually.
/// Report the hardware the test ran on together with the measured time.
/// </summary>
public class T11_MonthGridPerformanceTests : AttendanceTestBase
{
    private const int StudentCount = 30;
    private const int LessonCount = 120;

    private MonthGridService Grid => new(ContextFactory);

    private async Task<int> SeedLargeMonthAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);

        var group = new StudentGroup { Name = "ІПЗ-21", NormalizedName = "ІПЗ-21", AcademicYear = "2026-2027" };
        var subject = new Subject { Name = "Математика", NormalizedName = "МАТЕМАТИКА" };
        context.AddRange(group, subject);

        var students = Enumerable.Range(1, StudentCount)
            .Select(i => new Student { StudentGroup = group, FullName = $"Студент {i:00}" })
            .ToList();
        context.AddRange(students);

        // 30 days × 4 pairs = 120 lessons in November 2026.
        var lessons = Enumerable.Range(0, LessonCount)
            .Select(i => new Lesson
            {
                StudentGroup = group,
                Subject = subject,
                Date = new DateOnly(2026, 11, 1 + i / 4),
                PairNumber = 1 + i % 4,
            })
            .ToList();
        context.AddRange(lessons);

        var statuses = Enum.GetValues<AttendanceStatus>();
        var now = DateTime.UtcNow;
        for (var i = 0; i < lessons.Count; i++)
        {
            var lesson = lessons[i];
            for (var j = 0; j < students.Count; j++)
            {
                var student = students[j];
                var record = new AttendanceRecord { Lesson = lesson, Student = student };
                record.Versions.Add(new AttendanceVersion
                {
                    VersionNumber = 1,
                    Status = statuses[(i + j) % statuses.Length],
                    Grade = 60 + lesson.PairNumber * 5,
                    TimeStamp = now,
                });
                // Every tenth mark was edited once, so the "latest version" query has real work to do.
                if ((i + j) % 10 == 0)
                    record.Versions.Add(new AttendanceVersion
                    {
                        VersionNumber = 2,
                        Status = AttendanceStatus.Present,
                        Reason = "Виправлення",
                        TimeStamp = now,
                    });
                context.Add(record);
            }
        }

        await context.SaveChangesAsync(Ct);
        return group.StudentGroupId;
    }

    [Fact]
    public async Task MonthGrid_30Students_120Lessons_LoadsInUnderOneSecond()
    {
        var groupId = await SeedLargeMonthAsync();

        var stopwatch = Stopwatch.StartNew();
        var grid = await Grid.GetMonthGridAsync(groupId, null, 2026, 11);
        stopwatch.Stop();

        Assert.Equal(LessonCount, grid.Lessons.Count);
        Assert.Equal(StudentCount, grid.Rows.Count);
        Assert.All(grid.Rows, r => Assert.All(r.Cells, c => Assert.NotNull(c)));

        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Month grid 30x120: {stopwatch.ElapsedMilliseconds} ms on {Environment.ProcessorCount} logical CPUs");
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"The month grid took {stopwatch.ElapsedMilliseconds} ms; NFR-01 requires under 1000 ms.");
    }

    [Fact]
    public async Task MonthGrid_ShowsLatestVersion_LetterData_AndFiltersBySubject()
    {
        var groupId = await CreateGroupAsync();
        var math = await CreateSubjectAsync("Математика");
        var physics = await CreateSubjectAsync("Фізика");
        var studentId = await AddStudentAsync(groupId, "Шевченко Тарас");
        var archivedId = await AddStudentAsync(groupId, "Архівний Без Відміток");
        await Students.ArchiveAsync(archivedId);

        var l1 = await CreateLessonAsync(groupId, math, day: 2);
        await CreateLessonAsync(groupId, physics, day: 3);
        await MarkAsync(l1, studentId, AttendanceStatus.Absent);
        await MarkAsync(l1, studentId, AttendanceStatus.Excused, grade: 70, reason: "Довідка з лікарні");

        var all = await Grid.GetMonthGridAsync(groupId, null, 2026, 11);
        var mathOnly = await Grid.GetMonthGridAsync(groupId, math, 2026, 11);

        Assert.Equal(2, all.Lessons.Count);
        var row = Assert.Single(all.Rows); // the archived student without marks is hidden
        Assert.Equal(AttendanceStatus.Excused, row.Cells[0]!.Status);
        Assert.Equal(70, row.Cells[0]!.Grade);
        Assert.Null(row.Cells[1]);
        Assert.Equal("Математика", Assert.Single(mathOnly.Lessons).SubjectName);
    }
}
