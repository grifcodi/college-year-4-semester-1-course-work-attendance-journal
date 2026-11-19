using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

public partial class GroupListViewModel(
    IStudentGroupService groupService,
    IConfirmationService confirmation,
    IErrorLog errorLog) : OperationViewModelBase(errorLog)
{
    private int _loadVersion;

    public ObservableCollection<StudentGroup> Groups { get; } = [];

    [ObservableProperty]
    public partial StudentGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial bool ShowArchived { get; set; }

    [ObservableProperty]
    public partial string GroupNameInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AcademicYearInput { get; set; } = AcademicYearDefaults.Current();

    [ObservableProperty]
    public partial bool IsEditing { get; set; }
    public event Action<int?>? SelectedGroupIdChanged;

    partial void OnSelectedGroupChanged(StudentGroup? value)
    {
        ErrorMessage = null;
        CancelEdit();
        SelectedGroupIdChanged?.Invoke(value?.StudentGroupId);
    }

    partial void OnShowArchivedChanged(bool value) => _ = LoadAsync();

    public async Task LoadCoreAsync()
    {
        var version = ++_loadVersion;
        var previousId = SelectedGroup?.StudentGroupId;

        var items = await groupService.GetAllAsync(ShowArchived);
        if (version != _loadVersion)
            return;

        Groups.Clear();
        foreach (var item in items)
            Groups.Add(item);

        SelectedGroup = Groups.FirstOrDefault(g => g.StudentGroupId == previousId)
                        ?? Groups.FirstOrDefault();
    }

    public Task LoadAsync() => RunSafeAsync("GroupList.Load", LoadCoreAsync);

    [RelayCommand]
    private Task SaveAsync() => RunSafeAsync("GroupList.Save", async () =>
    {
        ErrorMessage = null;

        var result = IsEditing && SelectedGroup is not null
            ? await groupService.UpdateAsync(SelectedGroup.StudentGroupId, GroupNameInput, AcademicYearInput)
            : await groupService.CreateAsync(GroupNameInput, AcademicYearInput);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        CancelEdit();
        await LoadCoreAsync();
    });

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedGroup is null)
            return;

        GroupNameInput = SelectedGroup.Name;
        AcademicYearInput = SelectedGroup.AcademicYear;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        GroupNameInput = string.Empty;
        AcademicYearInput = AcademicYearDefaults.Current();
        IsEditing = false;
    }

    [RelayCommand]
    private Task ToggleArchiveAsync(StudentGroup? group) => RunSafeAsync("GroupList.ToggleArchive", async () =>
    {
        if (group is null)
            return;

        ErrorMessage = null;

        if (!group.IsArchived)
        {
            var message = Localizer.Translate(UiTextKeys.ConfirmArchiveGroup, group.Name);
            if (!await confirmation.ConfirmAsync(message))
                return;
        }

        var result = group.IsArchived
            ? await groupService.UnarchiveAsync(group.StudentGroupId)
            : await groupService.ArchiveAsync(group.StudentGroupId);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await LoadCoreAsync();
    });
}