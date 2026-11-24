using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using Microsoft.Data.Sqlite;

namespace AttendanceJournal.Tests;

public class T22_UkrainianSortingTests : AttendanceTestBase
{
    // Ukrainian alphabet: ... Г Ґ Д Е Є Ж ... И І Ї Й ... Я
    private static readonly string[] Expected =
        ["Андрієнко", "Бойко", "Ґудзь", "Єфремов", "Іваненко", "Їжак", "Яременко"];

    [Fact]
    public async Task Students_AreSortedByUkrainianAlphabet()
    {
        var groupId = await CreateGroupAsync();
        foreach (var name in new[] { "Іваненко", "Яременко", "Їжак", "Андрієнко", "Єфремов", "Бойко", "Ґудзь" })
            await AddStudentAsync(groupId, name);

        var students = await Students.GetByGroupAsync(groupId);

        Assert.Equal(Expected, students.Select(s => s.FullName));
    }

    [Fact]
    public async Task LessonSheet_AndSummary_UseTheSameOrder()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        foreach (var name in new[] { "Іваненко", "Андрієнко", "Єфремов" })
            await AddStudentAsync(groupId, name);
        var lessonId = await CreateLessonAsync(groupId, subjectId);

        var sheet = await Attendance.GetLessonSheetAsync(lessonId);
        var summary = await new SummaryService(ContextFactory).GetGroupSummaryAsync(groupId);

        string[] expected = ["Андрієнко", "Єфремов", "Іваненко"];
        Assert.Equal(expected, sheet.Select(r => r.FullName));
        Assert.Equal(expected, summary.Students.Select(s => s.FullName));
    }

    [Fact]
    public async Task Subjects_AreSortedByUkrainianAlphabet()
    {
        foreach (var name in new[] { "Інформатика", "Алгебра", "Етика" })
            await CreateSubjectAsync(name);

        var subjects = await Subjects.GetAllAsync();

        Assert.Equal(["Алгебра", "Етика", "Інформатика"], subjects.Select(s => s.Name));
    }
}

public class T22_SummaryArchivedStudentTests : SummaryTestBase
{
    [Fact]
    public async Task ArchivedStudentWithMarks_StillCountsInGroupRate_AndIsListed()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var active = await AddStudentAsync(groupId, "Активний Андрій");
        var archived = await AddStudentAsync(groupId, "Архівний Борис");
        var lessonId = await CreateLessonAsync(groupId, subjectId);

        await MarkAsync(lessonId, active, AttendanceStatus.Present);
        await MarkAsync(lessonId, archived, AttendanceStatus.Absent);
        await Students.ArchiveAsync(archived);

        var summary = await Summary.GetGroupSummaryAsync(groupId);

        Assert.Equal(2, summary.Students.Count);
        Assert.True(summary.Students.Single(s => s.FullName == "Архівний Борис").IsArchived);
        Assert.Equal(50.0, summary.GroupAttendanceRatePercent);
    }

    [Fact]
    public async Task ArchivedStudentWithoutMarks_IsHiddenByDefault_AndShownOnRequest()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await AddStudentAsync(groupId, "Активний Андрій");
        var archived = await AddStudentAsync(groupId, "Архівний Борис");
        await CreateLessonAsync(groupId, subjectId);
        await Students.ArchiveAsync(archived);

        var hidden = await Summary.GetGroupSummaryAsync(groupId);
        var shown = await Summary.GetGroupSummaryAsync(groupId, includeArchivedStudents: true);

        Assert.Equal("Активний Андрій", Assert.Single(hidden.Students).FullName);
        Assert.Equal(2, shown.Students.Count);
    }

    [Fact]
    public async Task GroupAverage_UsesUnroundedStudentAverages()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var s1 = await AddStudentAsync(groupId, "Перший");
        var s2 = await AddStudentAsync(groupId, "Другий");

        // s1: 100,100,99 -> 99.666..; s2: 90,90,90 -> 90.
        // Mean of raw averages = 94.83 -> 94.8 (mean of rounded 99.7 and 90.0 would give 94.9).
        var grades1 = new[] { 100, 100, 99 };
        for (var i = 0; i < 3; i++)
        {
            var lesson = await CreateLessonAsync(groupId, subjectId, day: i + 1);
            await MarkAsync(lesson, s1, AttendanceStatus.Present, grade: grades1[i]);
            await MarkAsync(lesson, s2, AttendanceStatus.Present, grade: 90);
        }

        var summary = await Summary.GetGroupSummaryAsync(groupId);

        Assert.Equal(94.8, summary.GroupAverageGrade);
        Assert.Equal(2, summary.QualifyingStudentsCount);
    }
}

public class T22_ErrorClassifierTests
{
    [Fact]
    public void ConstraintViolation_IsNotReportedAsUnavailableDatabase()
    {
        var ex = new SqliteException("CHECK constraint failed", 19);

        Assert.Null(DatabaseErrorClassifier.ClassifyErrorKey(ex));
    }

    [Fact]
    public void BusyAndLocked_StillMapToBusy()
    {
        Assert.Equal(ErrorKeys.DatabaseBusy, DatabaseErrorClassifier.ClassifyErrorKey(new SqliteException("busy", 5)));
        Assert.Equal(ErrorKeys.DatabaseBusy, DatabaseErrorClassifier.ClassifyErrorKey(new SqliteException("locked", 6)));
    }

    [Fact]
    public void OtherSqliteErrors_MapToUnavailable()
    {
        Assert.Equal(ErrorKeys.DatabaseUnavailable, DatabaseErrorClassifier.ClassifyErrorKey(new SqliteException("cannot open", 14)));
    }
}

[Collection(LocalizerCollection.Name)]
public class T22_SettingsRevertTests
{
    private sealed class FailingSettingService : ISettingService
    {
        public Task<AppSettingsSnapshot> GetSettingsAsync() =>
            Task.FromResult(new AppSettingsSnapshot(AppLanguage.Uk, AppTheme.Light));

        public Task<Result> SetLanguageAsync(AppLanguage language) =>
            Task.FromResult(Result.Failure(ErrorKeys.DatabaseBusy));

        public Task<Result> SetThemeAsync(AppTheme theme) =>
            Task.FromResult(Result.Failure(ErrorKeys.DatabaseBusy));
    }

    private sealed class RecordingThemeApplier : IThemeApplier
    {
        public List<AppTheme> Applied { get; } = [];
        public void Apply(AppTheme theme) => Applied.Add(theme);
    }

    [Fact]
    public async Task FailedLanguageSave_RevertsSelection_AndKeepsCurrentLanguage()
    {
        var original = Localizer.CurrentLanguage;
        try
        {
            Localizer.CurrentLanguage = AppLanguage.Uk;
            var vm = new SettingsPageViewModel(new FailingSettingService(), new RecordingThemeApplier(), new FakeErrorLog());
            vm.ApplySnapshot(new AppSettingsSnapshot(AppLanguage.Uk, AppTheme.Light));

            vm.SelectedLanguage = AppLanguage.En;

            await TestWait.UntilAsync(() => vm.ErrorMessage is not null && vm.SelectedLanguage == AppLanguage.Uk);
            Assert.Equal(AppLanguage.Uk, Localizer.CurrentLanguage);
        }
        finally
        {
            Localizer.CurrentLanguage = original;
        }
    }

    [Fact]
    public async Task FailedThemeSave_RevertsSelection_AndDoesNotApplyTheme()
    {
        var original = Localizer.CurrentLanguage;
        try
        {
            var applier = new RecordingThemeApplier();
            var vm = new SettingsPageViewModel(new FailingSettingService(), applier, new FakeErrorLog());
            vm.ApplySnapshot(new AppSettingsSnapshot(AppLanguage.Uk, AppTheme.Light));
            var appliedBefore = applier.Applied.Count;

            vm.SelectedTheme = AppTheme.Dark;

            await TestWait.UntilAsync(() => vm.ErrorMessage is not null && vm.SelectedTheme == AppTheme.Light);
            Assert.Equal(appliedBefore, applier.Applied.Count);
        }
        finally
        {
            Localizer.CurrentLanguage = original;
        }
    }
}

[Collection(LocalizerCollection.Name)]
public class T22_RetryTests
{
    private sealed class FlakyGroupService : IStudentGroupService
    {
        private int _calls;

        public Task<List<StudentGroup>> GetAllAsync(bool includeArchived = false)
        {
            if (++_calls == 1)
                throw new SqliteException("database is locked", 5);

            return Task.FromResult(new List<StudentGroup>
            {
                new() { StudentGroupId = 1, Name = "ІПЗ-21", NormalizedName = "ІПЗ-21", AcademicYear = "2026-2027" }
            });
        }

        public Task<Result<StudentGroup>> CreateAsync(string name, string academicYear) => throw new NotSupportedException();
        public Task<Result<StudentGroup>> UpdateAsync(int groupId, string newName, string newAcademicYear) => throw new NotSupportedException();
        public Task<Result> ArchiveAsync(int groupId) => throw new NotSupportedException();
        public Task<Result> UnarchiveAsync(int groupId) => throw new NotSupportedException();
    }

    [Fact]
    public async Task BusyDatabase_OffersRetry_AndRetrySucceeds()
    {
        var vm = new GroupListViewModel(new FlakyGroupService(), new FakeConfirmationService(), new FakeErrorLog());

        await vm.LoadAsync();

        Assert.True(vm.CanRetry);
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
        Assert.Empty(vm.Groups);

        await vm.RetryLastOperationCommand.ExecuteAsync(null);

        Assert.False(vm.CanRetry);
        Assert.Null(vm.ErrorMessage);
        Assert.Equal("ІПЗ-21", Assert.Single(vm.Groups).Name);
    }
}

public class T22_GroupSelectionTests : SqliteInMemoryTestBase
{
    [Fact]
    public async Task SavingNewGroup_SelectsIt()
    {
        var service = new StudentGroupService(ContextFactory);
        await service.CreateAsync("А-1", "2026-2027");
        var vm = new GroupListViewModel(service, new FakeConfirmationService(), new FakeErrorLog());
        await vm.LoadAsync();

        vm.GroupNameInput = "Я-9";
        vm.AcademicYearInput = "2025-2026";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Я-9", vm.SelectedGroup!.Name);
    }
}
