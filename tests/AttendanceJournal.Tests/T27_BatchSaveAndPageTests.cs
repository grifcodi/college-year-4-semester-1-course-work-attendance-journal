using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;
using System.Text;

namespace AttendanceJournal.Tests;

/// <summary>Batch save (NFR-02), month grid page (FR-08), summary export (FR-10), subjects page (FR-03), import page (FR-11).</summary>
[Collection(LocalizerCollection.Name)]
public class T27_BatchSaveAndPageTests : SummaryTestBase
{
    private readonly FakeConfirmationService _confirm = new();
    private readonly FakeErrorLog _log = new();
    private readonly FakeFileDialogService _files = new();

    private MonthGridPageViewModel CreateGridVm() => new(Groups, Subjects, new MonthGridService(ContextFactory), _log);

    private SummaryPageViewModel CreateSummaryVm() => new(
        Groups, Subjects, Summary, new AttendanceExportService(ContextFactory, Summary), _files, _log);

    [Fact]
    public async Task SaveMarks_WhenOneMarkFails_SavesNothing()
    {
        var (groupId, subjectId, studentId, lessonId) = await SeedOneStudentLessonAsync();
        var otherGroup = await CreateGroupAsync("КН-1");
        var stranger = await AddStudentAsync(otherGroup, "Чужий Студент");

        var result = await Attendance.SaveMarksAsync(lessonId,
        [
            new MarkInput(studentId, AttendanceStatus.Present, 90, null),
            new MarkInput(stranger, AttendanceStatus.Present, null, null),
        ], reason: null);

        Assert.Equal(ErrorKeys.AttendanceStudentWrongGroup, result.ErrorKey);
        Assert.Equal(0, await CountVersionsAsync());
        Assert.Equal(0, await CountRecordsAsync());
    }

    [Fact]
    public async Task SaveMarks_RejectsTheSameStudentTwice()
    {
        var (_, _, studentId, lessonId) = await SeedOneStudentLessonAsync();

        var result = await Attendance.SaveMarksAsync(lessonId,
        [
            new MarkInput(studentId, AttendanceStatus.Present, null, null),
            new MarkInput(studentId, AttendanceStatus.Absent, null, null),
        ], reason: null);

        Assert.Equal(ErrorKeys.AttendanceDuplicateStudentInBatch, result.ErrorKey);
        Assert.Equal(0, await CountVersionsAsync());
    }

    [Fact]
    public async Task MonthGridPage_LoadsMonth_AndOpeningACellRaisesEvent()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        var groupId = await SeedFixedDatasetAsync();
        var vm = CreateGridVm();
        vm.Year = 2026;
        vm.Month = 11;
        int? opened = null;
        vm.OpenLessonRequested += id => opened = id;

        await vm.ActivateAsync();

        Assert.Equal(groupId, vm.SelectedGroup!.StudentGroupId);
        Assert.Equal(4, vm.Columns.Count);
        Assert.Equal(4, vm.Rows.Count);
        var first = vm.Rows.Single(r => r.DisplayName == "Андрієнко Анна");
        Assert.Equal(["П", "З", "П", "Н"], first.Cells.Select(c => c.Letter));
        Assert.Equal("·", vm.Rows.Single(r => r.DisplayName == "Гриценко Гліб").Cells[0].Letter);

        vm.OpenCellCommand.Execute(first.Cells[1]);
        Assert.Equal(vm.Columns[1].LessonId, opened);

        await vm.PreviousMonthCommand.ExecuteAsync(null);
        Assert.Equal(10, vm.Month);
        Assert.True(vm.HasNoLessons);
    }

    [Fact]
    public async Task SummaryPage_ExportWritesExactlyTheRowCountShownOnScreen()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        await SeedFixedDatasetAsync();
        var vm = CreateSummaryVm();
        vm.FromDate = new DateTime(2026, 11, 1);
        vm.ToDate = new DateTime(2026, 11, 30);

        await vm.ActivateAsync();
        Assert.Equal(16, vm.ExportRowCount); // 4 lessons × 4 students, unmarked included
        Assert.Equal(4, vm.Rows.Count);

        await vm.ExportCsvCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        var text = Encoding.UTF8.GetString(_files.SavedBytes).TrimStart('﻿');
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(vm.ExportRowCount + 1, lines.Length);
        Assert.StartsWith("Група;Студент;Дата;Пара;Дисципліна;Статус;Оцінка;Коментар", lines[0]);
        Assert.Contains(lines, l => l.Contains("Не відмічено"));
    }

    [Fact]
    public async Task SummaryPage_ReversedRange_ShowsError()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        await SeedFixedDatasetAsync();
        var vm = CreateSummaryVm();
        await vm.ActivateAsync();

        vm.FromDate = new DateTime(2026, 12, 1);
        vm.ToDate = new DateTime(2026, 11, 1);
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(Localizer.TranslateError(Result.Failure(ErrorKeys.ExportInvalidRange)), vm.ErrorMessage);
        Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task SubjectsPage_CreatesRenamesAndArchives()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        var vm = new SubjectsPageViewModel(Subjects, _confirm, _log);
        await vm.ActivateAsync();

        vm.NameInput = "Бази даних";
        await vm.SaveCommand.ExecuteAsync(null);
        var subject = Assert.Single(vm.Subjects);

        vm.StartEditCommand.Execute(subject);
        vm.NameInput = "Бази даних і знань";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Бази даних і знань", Assert.Single(vm.Subjects).Name);

        vm.NameInput = "бази даних і знань";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage); // unique, case-insensitive

        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Subjects[0]);
        Assert.Empty(vm.Subjects);
        Assert.Single(_confirm.Messages);
    }

    [Fact]
    public async Task ImportPage_PreviewThenConfirm_ImportsAndRaisesEvent()
    {
        Localizer.CurrentLanguage = AppLanguage.Uk;
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var import = new StudentImportService(ContextFactory, new StudentFileReader());
        var vm = new StudentImportViewModel(import, _files, _confirm, _log);
        ImportSummary? imported = null;
        vm.Imported += s => { imported = s; return Task.CompletedTask; };
        _files.FileToOpen = FakeFileDialogService.FromText("list.csv", "FullName\nШевченко Тарас\nшевченко тарас\n");

        await vm.ChooseFileAsync(groupId);

        Assert.True(vm.IsPreviewOpen);
        Assert.Equal(2, vm.Rows.Count);
        Assert.True(vm.Rows[1].IsProblem);
        Assert.True(vm.CanConfirm);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.False(vm.IsPreviewOpen);
        Assert.Equal(1, imported!.CreatedStudents);
        Assert.Single(await Students.GetByGroupAsync(groupId));
    }
}
