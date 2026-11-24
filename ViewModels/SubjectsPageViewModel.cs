using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>List of subjects with add, rename, archive and restore (FR-03).</summary>
public partial class SubjectsPageViewModel(
    ISubjectService subjectService,
    IConfirmationService confirmation,
    IErrorLog errorLog) : OperationViewModelBase(errorLog), IActivatablePage
{
    private int _loadVersion;
    private int? _editingSubjectId;

    public ObservableCollection<Subject> Subjects { get; } = [];

    [ObservableProperty]
    public partial Subject? SelectedSubject { get; set; }

    [ObservableProperty]
    public partial bool ShowArchived { get; set; }

    [ObservableProperty]
    public partial string NameInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    partial void OnShowArchivedChanged(bool value) => _ = LoadAsync();

    public Task ActivateAsync() => LoadAsync();

    public Task LoadAsync() => RunSafeAsync("Subjects.Load", () => LoadCoreAsync(), allowRetry: true);

    public async Task LoadCoreAsync(int? selectSubjectId = null)
    {
        var version = ++_loadVersion;
        var previousId = selectSubjectId ?? SelectedSubject?.SubjectId;

        var items = await subjectService.GetAllAsync(ShowArchived);
        if (version != _loadVersion)
            return;

        Subjects.Clear();
        foreach (var item in items)
            Subjects.Add(item);

        SelectedSubject = Subjects.FirstOrDefault(s => s.SubjectId == previousId);
    }

    [RelayCommand]
    private Task SaveAsync() => RunSafeAsync("Subjects.Save", async () =>
    {
        ClearError();
        InfoMessage = null;

        var result = _editingSubjectId is int id
            ? await subjectService.UpdateAsync(id, NameInput)
            : await subjectService.CreateAsync(NameInput);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        CancelEdit();
        await LoadCoreAsync(result.Value?.SubjectId);
    }, allowRetry: true);

    [RelayCommand]
    private void StartEdit(Subject? subject)
    {
        if (subject is null)
            return;

        _editingSubjectId = subject.SubjectId;
        NameInput = subject.Name;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingSubjectId = null;
        NameInput = string.Empty;
        IsEditing = false;
    }

    [RelayCommand]
    private Task ToggleArchiveAsync(Subject? subject) => RunSafeAsync("Subjects.ToggleArchive", async () =>
    {
        if (subject is null)
            return;

        ClearError();
        InfoMessage = null;

        if (!subject.IsArchived)
        {
            var message = Localizer.Translate(UiTextKeys.ConfirmArchiveSubject, subject.Name);
            if (!await confirmation.ConfirmAsync(message))
                return;
        }

        var result = subject.IsArchived
            ? await subjectService.UnarchiveAsync(subject.SubjectId)
            : await subjectService.ArchiveAsync(subject.SubjectId);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await LoadCoreAsync();
    }, allowRetry: true);
}
