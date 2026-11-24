using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

public partial class StudentListViewModel(
    IStudentService studentService,
    IConfirmationService confirmation,
    IErrorLog errorLog) : OperationViewModelBase(errorLog)
{
    private int _loadVersion;
    private int? _editingStudentId;

    public ObservableCollection<Student> Students { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGroup))]
    public partial int? CurrentGroupId { get; set; }

    [ObservableProperty]
    public partial bool ShowArchived { get; set; }

    [ObservableProperty]
    public partial string FullNameInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    public bool HasGroup => CurrentGroupId is not null;

    partial void OnShowArchivedChanged(bool value) => _ = RefreshAsync();

    public Task LoadForGroupAsync(int? groupId)
    {
        CurrentGroupId = groupId;
        ClearError();
        CancelEdit();
        return RefreshAsync();
    }

    private Task RefreshAsync() => RunSafeAsync("StudentList.Load", async () =>
    {
        var version = ++_loadVersion;
        var groupId = CurrentGroupId;

        if (groupId is null)
        {
            Students.Clear();
            return;
        }

        var items = await studentService.GetByGroupAsync(groupId.Value, ShowArchived);
        if (version != _loadVersion)
            return;

        Students.Clear();
        foreach (var item in items)
            Students.Add(item);
    }, allowRetry: true);

    [RelayCommand]
    private Task SaveAsync() => RunSafeAsync("StudentList.Save", async () =>
    {
        if (CurrentGroupId is not int groupId)
            return;

        ClearError();

        var result = _editingStudentId is int studentId
            ? await studentService.UpdateAsync(studentId, FullNameInput)
            : await studentService.AddAsync(groupId, FullNameInput);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        CancelEdit();
        await RefreshAsync();
    }, allowRetry: true);

    [RelayCommand]
    private void StartEdit(Student? student)
    {
        if (student is null)
            return;

        _editingStudentId = student.StudentId;
        FullNameInput = student.FullName;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingStudentId = null;
        FullNameInput = string.Empty;
        IsEditing = false;
    }

    [RelayCommand]
    private Task ToggleArchiveAsync(Student? student) => RunSafeAsync("StudentList.ToggleArchive", async () =>
    {
        if (student is null)
            return;

        ClearError();

        if (!student.IsArchived)
        {
            var message = Localizer.Translate(UiTextKeys.ConfirmArchiveStudent, student.FullName);
            if (!await confirmation.ConfirmAsync(message))
                return;
        }

        var result = student.IsArchived
            ? await studentService.UnarchiveAsync(student.StudentId)
            : await studentService.ArchiveAsync(student.StudentId);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await RefreshAsync();
    }, allowRetry: true);
}


