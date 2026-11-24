using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>A row of the import preview with its result text (FR-11).</summary>
public sealed class ImportRowViewModel(ImportPreviewRow row)
{
    public int RowNumber { get; } = row.RowNumber;
    public string FullName { get; } = row.FullName;
    public string GroupName { get; } = row.GroupName;
    public bool IsProblem { get; } = row.Status != ImportRowStatus.Valid;

    public string ResultText { get; } = row.Status == ImportRowStatus.Valid
        ? Localizer.Translate(row.IsNewGroup ? UiTextKeys.ImportStatusNewGroup : UiTextKeys.ImportStatusValid)
        : Localizer.TranslateError(Result.Failure(row.ErrorKey!, row.ErrorArgs ?? []));
}

/// <summary>
/// Import of students: choose a file, see the preview with row numbers and reasons,
/// confirm new groups, import the valid rows in one transaction.
/// </summary>
public partial class StudentImportViewModel(
    IStudentImportService importService,
    IFileDialogService fileDialogs,
    IConfirmationService confirmation,
    IErrorLog errorLog) : OperationViewModelBase(errorLog)
{
    private ImportPreview? _preview;

    public ObservableCollection<ImportRowViewModel> Rows { get; } = [];

    /// <summary>Raised after a successful import so the page can reload groups and students.</summary>
    public event Func<ImportSummary, Task>? Imported;

    [ObservableProperty]
    public partial bool IsPreviewOpen { get; set; }

    [ObservableProperty]
    public partial string PreviewTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanConfirm { get; set; }

    /// <summary>Opens the file dialog and builds the preview. Rows without a group go to <paramref name="defaultGroupId"/>.</summary>
    public Task ChooseFileAsync(int? defaultGroupId) => RunSafeAsync("Import.Preview", async () =>
    {
        ClearError();
        InfoMessage = null;

        var file = await fileDialogs.PickFileToOpenAsync(
            Localizer.Translate(UiTextKeys.ImportDialogTitle),
            Localizer.Translate(UiTextKeys.ImportFileTypeName),
            ["csv", "xlsx"]);

        if (file is null)
            return;

        await using var stream = await file.OpenAsync();
        await PreviewCoreAsync(stream, file.Name, defaultGroupId);
    }, allowRetry: true);

    /// <summary>Builds the preview from a stream (used by the dialog flow and by tests).</summary>
    public async Task PreviewCoreAsync(Stream stream, string fileName, int? defaultGroupId)
    {
        var result = await importService.PreviewAsync(stream, fileName, defaultGroupId);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            Close();
            return;
        }

        _preview = result.Value!;

        Rows.Clear();
        foreach (var row in _preview.Rows)
            Rows.Add(new ImportRowViewModel(row));

        PreviewTitle = Localizer.Translate(UiTextKeys.ImportPreviewHeader, _preview.FileName);
        CountsText = Localizer.Translate(
            UiTextKeys.ImportPreviewCounts, _preview.ValidCount, _preview.DuplicateCount, _preview.InvalidCount);
        ConfirmText = Localizer.Translate(UiTextKeys.ImportConfirmButton, _preview.ValidCount);
        CanConfirm = _preview.ValidCount > 0;
        IsPreviewOpen = true;
    }

    [RelayCommand]
    private Task ConfirmAsync() => RunSafeAsync("Import.Confirm", async () =>
    {
        if (_preview is null)
            return;

        ClearError();

        var academicYear = AcademicYearDefaults.Current();
        var createNewGroups = true;
        if (_preview.NewGroupNames.Count > 0)
        {
            var message = Localizer.Translate(
                UiTextKeys.ImportConfirmNewGroups,
                string.Join(", ", _preview.NewGroupNames),
                academicYear);
            createNewGroups = await confirmation.ConfirmAsync(message);
        }

        var result = await importService.ImportAsync(_preview, createNewGroups, academicYear);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        var summary = result.Value!;
        Close();
        ShowInfo(Localizer.Translate(
            UiTextKeys.ImportDoneInfo,
            summary.CreatedStudents,
            summary.CreatedGroups,
            summary.SkippedDuplicates,
            summary.InvalidRows));

        if (Imported is not null)
            await Imported(summary);
    }, allowRetry: true);

    [RelayCommand]
    private void Cancel()
    {
        ClearError();
        Close();
    }

    private void Close()
    {
        _preview = null;
        Rows.Clear();
        IsPreviewOpen = false;
        CanConfirm = false;
    }
}
