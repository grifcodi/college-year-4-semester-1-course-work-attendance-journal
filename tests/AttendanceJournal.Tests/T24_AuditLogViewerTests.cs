using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

/// <summary>T-09 (viewer part, FR-12): filters, order by Id, limit.</summary>
public class T24_AuditLogViewerTests : AttendanceTestBase
{
    private AuditLogService Audit => new(ContextFactory);

    [Fact]
    public async Task Entries_AreNewestFirst_ByAuditEntryId()
    {
        var groupId = await CreateGroupAsync();
        await AddStudentAsync(groupId, "Шевченко Тарас");
        await CreateSubjectAsync();

        var page = await Audit.GetEntriesAsync(new AuditLogFilter());

        Assert.Equal(3, page.TotalCount);
        var ids = page.Entries.Select(e => e.AuditEntryId).ToList();
        Assert.Equal(ids.OrderByDescending(i => i), ids);
        Assert.Equal(AuditEntityType.Subject, page.Entries[0].EntityType);
        Assert.All(page.Entries, e => Assert.Equal(DateTimeKind.Utc, e.TimeStampUtc.Kind));
    }

    [Fact]
    public async Task Filters_ByEntityType_AndAction()
    {
        var groupId = await CreateGroupAsync();
        var studentId = await AddStudentAsync(groupId, "Шевченко Тарас");
        await Students.ArchiveAsync(studentId);
        await Groups.ArchiveAsync(groupId);

        var students = await Audit.GetEntriesAsync(new AuditLogFilter(EntityType: AuditEntityType.Student));
        var archives = await Audit.GetEntriesAsync(new AuditLogFilter(Action: AuditAction.Archive));
        var studentArchives = await Audit.GetEntriesAsync(
            new AuditLogFilter(EntityType: AuditEntityType.Student, Action: AuditAction.Archive));

        Assert.Equal(2, students.TotalCount);
        Assert.Equal(2, archives.TotalCount);
        Assert.Equal(studentId, Assert.Single(studentArchives.Entries).EntityId);
    }

    [Fact]
    public async Task Filters_ByDateRange_InLocalDays()
    {
        await CreateGroupAsync();
        var today = DateOnly.FromDateTime(DateTime.Now);

        var todayOnly = await Audit.GetEntriesAsync(new AuditLogFilter(today, today));
        var tomorrowOn = await Audit.GetEntriesAsync(new AuditLogFilter(FromDate: today.AddDays(1)));
        var untilYesterday = await Audit.GetEntriesAsync(new AuditLogFilter(ToDate: today.AddDays(-1)));

        Assert.Equal(1, todayOnly.TotalCount);
        Assert.Equal(0, tomorrowOn.TotalCount);
        Assert.Equal(0, untilYesterday.TotalCount);
    }

    [Fact]
    public async Task MaxEntries_LimitsTheList_ButTotalCountsEverything()
    {
        for (var i = 0; i < 5; i++)
            await CreateSubjectAsync($"Дисципліна {i}");

        var page = await Audit.GetEntriesAsync(new AuditLogFilter(MaxEntries: 2));

        Assert.Equal(2, page.Entries.Count);
        Assert.Equal(5, page.TotalCount);
    }

    [Fact]
    public async Task BulkMarkAllPresent_IsOneEntry_OnTheLesson()
    {
        var (_, _, _, lessonId) = await SeedOneStudentLessonAsync();

        await Attendance.MarkAllPresentAsync(lessonId);
        var page = await Audit.GetEntriesAsync(new AuditLogFilter(Action: AuditAction.MarkAttendance));

        var entry = Assert.Single(page.Entries);
        Assert.Equal(AuditEntityType.Lesson, entry.EntityType);
        Assert.Equal(lessonId, entry.EntityId);
    }
}
