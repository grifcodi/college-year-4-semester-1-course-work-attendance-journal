using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

/// <summary>Repeating lessons (FR-04 extension): one weekday, one pair, between two dates.</summary>
[Collection(LocalizerCollection.Name)]
public class T28_LessonSeriesTests : AttendanceTestBase
{
    // 1 Sep 2026 is a Tuesday; Wednesdays from 2 Sep to 30 Dec 2026 = 18 dates.
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly End = new(2026, 12, 31);

    private async Task<(int GroupId, int SubjectId)> SeedAsync()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var subjectId = await CreateSubjectAsync("Бази даних");
        return (groupId, subjectId);
    }

    private static LessonSeriesRequest Wednesdays(int groupId, int subjectId, int interval = 1, int pair = 2) =>
        new(groupId, subjectId, DayOfWeek.Wednesday, pair, Start, End, interval);

    private async Task<int> CountLessonsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.Lessons.CountAsync(Ct);
    }

    [Fact]
    public async Task WeeklySeries_CreatesEveryWednesday_InOneAuditEntry()
    {
        var (groupId, subjectId) = await SeedAsync();

        var result = await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId));

        Assert.True(result.IsSuccess);
        Assert.Equal(18, result.Value!.CreatedLessonIds.Count);
        Assert.Empty(result.Value.SkippedDates);

        var lessons = await Lessons.GetByRangeAsync(groupId, Start, End);
        Assert.Equal(18, lessons.Count);
        Assert.All(lessons, l =>
        {
            Assert.Equal(DayOfWeek.Wednesday, l.Date.DayOfWeek);
            Assert.Equal(2, l.PairNumber);
            Assert.Equal(subjectId, l.SubjectId);
        });
        Assert.Equal(new DateOnly(2026, 9, 2), lessons[0].Date);
        Assert.Equal(new DateOnly(2026, 12, 30), lessons[^1].Date);

        var entry = Assert.Single(await ContextFactory.GetAuditAsync(), a => a.EntityType == AuditEntityType.Lesson);
        Assert.Equal(AuditAction.CreateSeries, entry.Action);
    }

    [Fact]
    public async Task EveryTwoWeeks_CreatesEveryOtherWednesday()
    {
        var (groupId, subjectId) = await SeedAsync();

        var result = await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId, interval: 2));

        Assert.Equal(9, result.Value!.CreatedLessonIds.Count);
        var dates = (await Lessons.GetByRangeAsync(groupId, Start, End)).Select(l => l.Date).ToList();
        Assert.All(dates.Zip(dates.Skip(1)), pair => Assert.Equal(14, pair.Second.DayNumber - pair.First.DayNumber));
    }

    [Fact]
    public async Task TakenDates_AreSkipped_AndReported()
    {
        var (groupId, subjectId) = await SeedAsync();
        await Lessons.CreateAsync(groupId, subjectId, new DateOnly(2026, 10, 7), 2, "Контрольна");
        await Lessons.CreateAsync(groupId, subjectId, new DateOnly(2026, 10, 14), 3, null); // other pair: not a conflict

        var plan = await Lessons.PlanSeriesAsync(Wednesdays(groupId, subjectId));
        var result = await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId));

        Assert.Equal([new DateOnly(2026, 10, 7)], plan.Value!.TakenDates);
        Assert.Equal(17, plan.Value.NewDates.Count);
        Assert.Equal(17, result.Value!.CreatedLessonIds.Count);
        Assert.Equal([new DateOnly(2026, 10, 7)], result.Value.SkippedDates);
        Assert.Equal(19, await CountLessonsAsync());
    }

    [Fact]
    public async Task Plan_DoesNotChangeTheDatabase()
    {
        var (groupId, subjectId) = await SeedAsync();

        var plan = await Lessons.PlanSeriesAsync(Wednesdays(groupId, subjectId));

        Assert.Equal(18, plan.Value!.NewDates.Count);
        Assert.Equal(0, await CountLessonsAsync());
    }

    [Fact]
    public async Task InvalidRequests_GiveClearErrors_AndCreateNothing()
    {
        var (groupId, subjectId) = await SeedAsync();
        var ok = Wednesdays(groupId, subjectId);

        Assert.Equal(ErrorKeys.LessonSeriesInvalidRange,
            (await Lessons.CreateSeriesAsync(ok with { StartDate = End, EndDate = Start })).ErrorKey);
        Assert.Equal(ErrorKeys.LessonSeriesTooLong,
            (await Lessons.CreateSeriesAsync(ok with { EndDate = Start.AddDays(367) })).ErrorKey);
        Assert.Equal(ErrorKeys.LessonSeriesNoDates,
            (await Lessons.CreateSeriesAsync(ok with { EndDate = Start })).ErrorKey); // a Tuesday only
        Assert.Equal(ErrorKeys.LessonInvalidPairNumber,
            (await Lessons.CreateSeriesAsync(ok with { PairNumber = 9 })).ErrorKey);
        Assert.Equal(ErrorKeys.LessonSeriesInvalidInterval,
            (await Lessons.CreateSeriesAsync(ok with { WeekInterval = 0 })).ErrorKey);

        Assert.Equal(0, await CountLessonsAsync());
    }

    [Fact]
    public async Task FullYear_IsAllowed()
    {
        var (groupId, subjectId) = await SeedAsync();

        var result = await Lessons.CreateSeriesAsync(
            Wednesdays(groupId, subjectId) with { StartDate = new(2026, 9, 1), EndDate = new(2027, 8, 31) });

        Assert.True(result.IsSuccess);
        Assert.Equal(52, result.Value!.CreatedLessonIds.Count);
    }

    [Fact]
    public async Task AllDatesTaken_OrArchivedGroup_Fails()
    {
        var (groupId, subjectId) = await SeedAsync();
        await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId));

        var again = await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId));
        Assert.Equal(ErrorKeys.LessonSeriesAllTaken, again.ErrorKey);

        await Groups.ArchiveAsync(groupId);
        var archived = await Lessons.CreateSeriesAsync(Wednesdays(groupId, subjectId, pair: 5));
        Assert.Equal(ErrorKeys.EntityNotFound, archived.ErrorKey);
    }

    [Fact]
    public async Task JournalPage_CreatesSeriesAfterConfirmation()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        var (groupId, _) = await SeedAsync();
        var confirm = new FakeConfirmationService();
        var vm = new JournalPageViewModel(Groups, Subjects, Lessons, Attendance, confirm, new FakeErrorLog())
        {
            SelectedDate = new DateTime(2026, 9, 2) // before activation: no group yet, so no background load
        };
        await vm.ActivateAsync();

        vm.IsSeriesMode = true;
        Assert.Equal(DayOfWeek.Wednesday, vm.SelectedSeriesDay!.Value);
        Assert.Equal(new DateTime(2026, 12, 31), vm.SeriesEndDate);

        confirm.Answer = false;
        await vm.CreateSeriesCommand.ExecuteAsync(null);
        Assert.Equal(0, await CountLessonsAsync());
        Assert.Contains("18", Assert.Single(confirm.Messages));

        confirm.Answer = true;
        await vm.CreateSeriesCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(18, await CountLessonsAsync());
        Assert.Equal(Localizer.Translate(UiTextKeys.SeriesCreatedInfo, 18, 0), vm.InfoMessage);
        Assert.Single(vm.Lessons); // 2 Sep is one of the dates
        Assert.Equal(groupId, vm.SelectedGroup!.StudentGroupId);
    }

    [Theory]
    [InlineData(2026, 9, 2, 2026, 12, 31)]
    [InlineData(2027, 2, 10, 2027, 6, 30)]
    public void SemesterEnd_IsEndOfDecemberOrJune(int y, int m, int d, int ey, int em, int ed)
    {
        Assert.Equal(new DateTime(ey, em, ed), JournalPageViewModel.SemesterEnd(new DateTime(y, m, d)));
    }
}
