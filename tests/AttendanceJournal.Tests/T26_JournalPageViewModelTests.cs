using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;

namespace AttendanceJournal.Tests;

/// <summary>Journal screen (FR-04 – FR-07): create a lesson, mark, save in one action, edit with a reason.</summary>
[Collection(LocalizerCollection.Name)]
public class T26_JournalPageViewModelTests : AttendanceTestBase
{
    private readonly FakeConfirmationService _confirm = new();
    private readonly FakeErrorLog _log = new();

    private JournalPageViewModel CreateVm() => new(Groups, Subjects, Lessons, Attendance, _confirm, _log);

    private static string Uk(string key, params object[] args) =>
        Localizer.TranslateError(Result.Failure(key, args), AppLanguage.Uk);

    /// <summary>A group with three students and a subject; the page is activated and a lesson is created for today.</summary>
    private async Task<JournalPageViewModel> OpenLessonWithThreeStudentsAsync()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        var groupId = await CreateGroupAsync("ІПЗ-21");
        await CreateSubjectAsync("Математика");
        await AddStudentAsync(groupId, "Андрієнко Анна");
        await AddStudentAsync(groupId, "Бойко Богдан");
        await AddStudentAsync(groupId, "Василенко Віра");

        var vm = CreateVm();
        await vm.ActivateAsync();
        vm.NewLessonTopic = "Вступ";
        await vm.CreateLessonCommand.ExecuteAsync(null);
        return vm;
    }

    [Fact]
    public async Task CreateLesson_SelectsIt_AndShowsAllStudentsAsNotMarked()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();

        Assert.Null(vm.ErrorMessage);
        Assert.NotNull(vm.SelectedLesson);
        Assert.Equal("Вступ", vm.TopicInput);
        Assert.Equal(3, vm.Rows.Count);
        Assert.All(vm.Rows, r => Assert.Null(r.Status));
        Assert.Equal(0, vm.UnsavedCount);
        Assert.Equal(2, vm.NewLessonPair); // pair 1 is taken now
    }

    [Fact]
    public async Task SaveMarks_SavesAllChangedRows_InOneAction()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();
        vm.Rows[0].Status = AttendanceStatus.Present;
        vm.Rows[0].GradeText = "90";
        vm.Rows[1].Status = AttendanceStatus.Absent;
        vm.Rows[1].NoteText = "Хворіє";

        Assert.Equal(2, vm.UnsavedCount);
        Assert.False(vm.IsReasonRequired);

        await vm.SaveMarksCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(0, vm.UnsavedCount);
        Assert.Equal(2, await CountVersionsAsync());
        Assert.Equal(90, vm.Rows[0].Saved!.Grade);
        Assert.Equal("Хворіє", vm.Rows[1].Saved!.Note);
        Assert.Null(vm.Rows[2].Saved);
        Assert.Equal(Localizer.Translate(UiTextKeys.SavedMarksInfo, 2), vm.InfoMessage);
    }

    [Theory]
    [InlineData("12.5")]
    [InlineData("101")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task InvalidGrade_BlocksSaving_AndShowsRowError(string grade)
    {
        var vm = await OpenLessonWithThreeStudentsAsync();
        vm.Rows[0].Status = AttendanceStatus.Present;
        vm.Rows[1].Status = AttendanceStatus.Present;
        vm.Rows[1].GradeText = grade;

        await vm.SaveMarksCommand.ExecuteAsync(null);

        Assert.Equal(Uk(ErrorKeys.AttendanceInvalidGrade), vm.Rows[1].RowError);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Equal(0, await CountVersionsAsync());
    }

    [Fact]
    public async Task GradeWithoutStatus_AsksForStatus()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();
        vm.Rows[0].GradeText = "80";

        await vm.SaveMarksCommand.ExecuteAsync(null);

        Assert.Equal(Localizer.Translate(UiTextKeys.RowStatusRequired), vm.Rows[0].RowError);
        Assert.Equal(0, await CountVersionsAsync());
    }

    [Fact]
    public async Task EditingSavedMark_RequiresReason_ThenCreatesVersion2()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();
        vm.Rows[0].Status = AttendanceStatus.Absent;
        await vm.SaveMarksCommand.ExecuteAsync(null);

        vm.Rows[0].Status = AttendanceStatus.Excused;
        Assert.True(vm.IsReasonRequired);

        await vm.SaveMarksCommand.ExecuteAsync(null);
        Assert.Equal(Uk(ErrorKeys.AttendanceReasonRequired), vm.ErrorMessage);

        vm.ReasonInput = "абв";
        await vm.SaveMarksCommand.ExecuteAsync(null);
        Assert.Equal(Uk(ErrorKeys.AttendanceReasonTooShort, ValidationConstants.ReasonMinLength), vm.ErrorMessage);
        Assert.Equal(1, await CountVersionsAsync());

        vm.ReasonInput = "Принесла довідку";
        await vm.SaveMarksCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(2, vm.Rows[0].Saved!.VersionNumber);
        Assert.Equal(AttendanceStatus.Excused, vm.Rows[0].Saved!.Status);
        Assert.Equal(string.Empty, vm.ReasonInput);
    }

    [Fact]
    public async Task MarkAllPresent_MarksEveryone_AndDiscardResetsEdits()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();

        await vm.MarkAllPresentCommand.ExecuteAsync(null);
        Assert.All(vm.Rows, r => Assert.Equal(AttendanceStatus.Present, r.Saved!.Status));

        vm.Rows[2].Status = AttendanceStatus.Late;
        Assert.Equal(1, vm.UnsavedCount);
        vm.DiscardChangesCommand.Execute(null);

        Assert.Equal(AttendanceStatus.Present, vm.Rows[2].Status);
        Assert.Equal(0, vm.UnsavedCount);
    }

    [Fact]
    public async Task KeyboardShortcut_SetsRowStatus()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();

        JournalPageViewModel.SetStatusByShortcut(vm.Rows[0], 3);
        JournalPageViewModel.SetStatusByShortcut(vm.Rows[1], 4);

        Assert.Equal(AttendanceStatus.Late, vm.Rows[0].Status);
        Assert.Equal(AttendanceStatus.Excused, vm.Rows[1].Status);
    }

    [Fact]
    public async Task History_ShowsVersions_AndRestoreCreatesNewVersion()
    {
        var vm = await OpenLessonWithThreeStudentsAsync();
        vm.Rows[0].Status = AttendanceStatus.Absent;
        await vm.SaveMarksCommand.ExecuteAsync(null);
        vm.Rows[0].Status = AttendanceStatus.Present;
        vm.ReasonInput = "Помилка введення";
        await vm.SaveMarksCommand.ExecuteAsync(null);

        await vm.ShowHistoryCommand.ExecuteAsync(vm.Rows[0]);
        Assert.True(vm.IsHistoryOpen);
        Assert.Equal([2, 1], vm.HistoryItems.Select(h => h.VersionNumber));

        await vm.RestoreVersionCommand.ExecuteAsync(vm.HistoryItems.Single(h => h.VersionNumber == 1));

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(3, vm.Rows[0].Saved!.VersionNumber);
        Assert.Equal(AttendanceStatus.Absent, vm.Rows[0].Saved!.Status);
    }

    [Fact]
    public async Task OpenLesson_SelectsGroupDateAndLesson()
    {
        var (groupId, subjectId, _, _) = await SeedOneStudentLessonAsync();
        var lessonId = await CreateLessonAsync(groupId, subjectId, day: 12, pair: 3);
        var vm = CreateVm();

        await vm.OpenLessonAsync(lessonId);

        Assert.Equal(groupId, vm.SelectedGroup!.StudentGroupId);
        Assert.Equal(new DateTime(2026, 11, 12), vm.SelectedDate);
        Assert.Equal(lessonId, vm.SelectedLesson!.LessonId);
        Assert.Single(vm.Rows);
    }

    [Theory]
    [InlineData("", true, null)]
    [InlineData(" 85 ", true, 85)]
    [InlineData("0", true, 0)]
    [InlineData("100", true, 100)]
    [InlineData("85,5", false, null)]
    [InlineData("+5", false, null)]
    public void TryParseGrade_AcceptsOnlyWholeNumbers0To100(string text, bool ok, int? expected)
    {
        Assert.Equal(ok, MarkRowViewModel.TryParseGrade(text, out var grade));
        Assert.Equal(expected, grade);
    }
}
