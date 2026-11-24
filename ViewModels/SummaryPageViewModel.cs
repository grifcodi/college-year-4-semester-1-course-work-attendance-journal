using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>A student line of the summary table, already formatted for display.</summary>
public sealed class SummaryRowViewModel(StudentSummaryDto s)
{
    public string DisplayName { get; } = DisplayText.WithArchiveMark(s.FullName, s.IsArchived);
    public int Present { get; } = s.PresentCount;
    public int Late { get; } = s.LateCount;
    public int Absent { get; } = s.AbsentCount;
    public int Excused { get; } = s.ExcusedCount;
    public int Unmarked { get; } = s.UnmarkedCount;
    public string RateText { get; } = DisplayText.Percent(s.AttendanceRatePercent);
    public int GradesCount { get; } = s.GradedLessonsCount;

    // Average grade is shown only with at least 3 grades (spec 5.4); otherwise '—' and the count.
    public string AverageText { get; } = s.AverageGrade is double avg
        ? DisplayText.Grade(avg)
        : Localizer.Translate(UiTextKeys.AverageNeedsGrades, s.GradedLessonsCount);
}

/// <summary>Summary per student and per group for a date range (FR-09) and export (FR-10).</summary>
public partial class SummaryPageViewModel(
    IStudentGroupService groupService,
    ISubjectService subjectService,
    ISummaryService summaryService,
    IAttendanceExportService exportService,
    IFileDialogService fileDialogs,
    IErrorLog errorLog) : OperationViewModelBase(errorLog), IActivatablePage
{
    private bool _suppressReload;
    private int _loadVersion;

    public ObservableCollection<StudentGroup> Groups { get; } = [];
    public ObservableCollection<SelectOption<int?>> SubjectOptions { get; } = [];
    public ObservableCollection<SummaryRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    public partial StudentGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial bool ShowArchivedGroups { get; set; }

    [ObservableProperty]
    public partial SelectOption<int?>? SelectedSubjectOption { get; set; }

    [ObservableProperty]
    public partial DateTime? FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    public partial DateTime? ToDate { get; set; } =
        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);

    [ObservableProperty]
    public partial bool IncludeArchivedStudents { get; set; }

    [ObservableProperty]
    public partial string GroupTotalsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ExportRowCount { get; set; }

    [ObservableProperty]
    public partial string ExportRowsText { get; set; } = string.Empty;

    partial void OnSelectedGroupChanged(StudentGroup? value) => ReloadIfAllowed();
    partial void OnSelectedSubjectOptionChanged(SelectOption<int?>? value) => ReloadIfAllowed();
    partial void OnFromDateChanged(DateTime? value) => ReloadIfAllowed();
    partial void OnToDateChanged(DateTime? value) => ReloadIfAllowed();
    partial void OnIncludeArchivedStudentsChanged(bool value) => ReloadIfAllowed();
    partial void OnShowArchivedGroupsChanged(bool value) => _ = ActivateAsync();

    private void ReloadIfAllowed()
    {
        if (!_suppressReload)
            _ = LoadAsync();
    }

    public Task ActivateAsync() => RunSafeAsync("Summary.Activate", async () =>
    {
        var groups = await groupService.GetAllAsync(ShowArchivedGroups);
        var subjects = await subjectService.GetAllAsync(includeArchived: true);

        _suppressReload = true;
        try
        {
            var groupId = SelectedGroup?.StudentGroupId;
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);
            SelectedGroup = Groups.FirstOrDefault(g => g.StudentGroupId == groupId) ?? Groups.FirstOrDefault();

            var subjectId = SelectedSubjectOption?.Value;
            SubjectOptions.Clear();
            SubjectOptions.Add(new SelectOption<int?>(null, Localizer.Translate(UiTextKeys.AllSubjects)));
            foreach (var s in subjects)
                SubjectOptions.Add(new SelectOption<int?>(s.SubjectId, DisplayText.WithArchiveMark(s.Name, s.IsArchived)));
            SelectedSubjectOption = SubjectOptions.FirstOrDefault(o => o.Value == subjectId) ?? SubjectOptions[0];
        }
        finally
        {
            _suppressReload = false;
        }

        await LoadCoreAsync();
    }, allowRetry: true);

    [RelayCommand]
    public Task LoadAsync() => RunSafeAsync("Summary.Load", LoadCoreAsync, allowRetry: true);

    private async Task LoadCoreAsync()
    {
        var version = ++_loadVersion;
        ClearError();

        if (SelectedGroup is not { } group || !TryGetRange(out var from, out var to))
        {
            Rows.Clear();
            GroupTotalsText = string.Empty;
            ExportRowCount = 0;
            ExportRowsText = string.Empty;
            return;
        }

        var subjectId = SelectedSubjectOption?.Value;
        var summary = await summaryService.GetGroupSummaryAsync(
            group.StudentGroupId, subjectId, from, to, IncludeArchivedStudents);
        var exportRows = await exportService.GetRowsAsync(group.StudentGroupId, subjectId, from, to);

        if (version != _loadVersion)
            return;

        Rows.Clear();
        foreach (var s in summary.Students)
            Rows.Add(new SummaryRowViewModel(s));

        GroupTotalsText = Localizer.Translate(
            UiTextKeys.GroupTotalsLine,
            DisplayText.Percent(summary.GroupAttendanceRatePercent),
            DisplayText.Grade(summary.GroupAverageGrade),
            summary.QualifyingStudentsCount,
            summary.TotalLessonsCount);

        // The file gets exactly this many data rows (FR-10 acceptance criterion).
        ExportRowCount = exportRows.Count;
        ExportRowsText = Localizer.Translate(UiTextKeys.ExportRowsCount, exportRows.Count);
    }

    /// <summary>Validates the range. Shows an error and returns false if the start is after the end.</summary>
    private bool TryGetRange(out DateOnly from, out DateOnly to)
    {
        from = FromDate is DateTime f ? DateOnly.FromDateTime(f) : DateOnly.MinValue;
        to = ToDate is DateTime t ? DateOnly.FromDateTime(t) : DateOnly.MaxValue;

        if (from <= to)
            return true;

        ErrorMessage = Localizer.TranslateError(Result.Failure(ErrorKeys.ExportInvalidRange));
        return false;
    }

    [RelayCommand]
    private Task ExportCsvAsync() => ExportAsync(ExportFormat.Csv);

    [RelayCommand]
    private Task ExportXlsxAsync() => ExportAsync(ExportFormat.Xlsx);

    private Task ExportAsync(ExportFormat format) => RunSafeAsync("Summary.Export", async () =>
    {
        ClearError();
        InfoMessage = null;

        if (SelectedGroup is not { } group || !TryGetRange(out var from, out var to))
            return;

        var extension = format == ExportFormat.Csv ? "csv" : "xlsx";
        var suggestedName = $"{group.Name}_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.{extension}";
        if (FromDate is null || ToDate is null)
            suggestedName = $"{group.Name}.{extension}";

        var file = await fileDialogs.PickFileToSaveAsync(
            Localizer.Translate(UiTextKeys.ExportDialogTitle),
            SafeFileName(suggestedName),
            extension.ToUpperInvariant(),
            extension);

        if (file is null)
            return;

        await using var stream = await file.OpenAsync();
        var result = await exportService.ExportAsync(
            group.StudentGroupId,
            SelectedSubjectOption?.Value,
            from,
            to,
            format,
            DisplayText.CreateExportLabels(),
            stream);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        ShowInfo(Localizer.Translate(UiTextKeys.ExportDoneInfo, result.Value, file.Name));
    }, allowRetry: true);

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
