using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AttendanceJournal.ViewModels;

public partial class GroupsPageViewModel : ViewModelBase, IActivatablePage
{
    private Func<Task>? _retryAction;

    public GroupsPageViewModel(
        GroupListViewModel groupList,
        StudentListViewModel studentList,
        StudentImportViewModel import)
    {
        GroupList = groupList;
        StudentList = studentList;
        Import = import;
        IsInitializing = true;

        GroupList.SelectedGroupIdChanged += id => _ = StudentList.LoadForGroupAsync(id);
        Import.Imported += OnImportedAsync;
    }

    public GroupListViewModel GroupList { get; }
    public StudentListViewModel StudentList { get; }
    public StudentImportViewModel Import { get; }

    /// <summary>Reloads groups when the page is opened again (other pages may have changed data).</summary>
    public Task ActivateAsync() =>
        IsInitializing || StartupErrorMessage is not null ? Task.CompletedTask : GroupList.LoadAsync();

    /// <summary>Import: rows without a Group column go to the group selected in the list.</summary>
    [RelayCommand]
    private Task ImportStudentsAsync() => Import.ChooseFileAsync(GroupList.SelectedGroup?.StudentGroupId);

    private async Task OnImportedAsync(ImportSummary summary)
    {
        await GroupList.LoadAsync();
        await StudentList.LoadForGroupAsync(GroupList.SelectedGroup?.StudentGroupId);
    }

    [ObservableProperty]
    public partial bool IsInitializing { get; set; }

    [ObservableProperty]
    public partial string? StartupErrorMessage { get; set; }

    public void SetStartupFailure(string localizedMessage, Func<Task> retryAction)
    {
        IsInitializing = false;
        StartupErrorMessage = localizedMessage;
        _retryAction = retryAction;
    }
    public async Task InitializeAsync()
    {
        StartupErrorMessage = null;
        IsInitializing = true;
        try
        {
            await GroupList.LoadCoreAsync();
        }
        finally
        {
            IsInitializing = false;
        }
    }

    [RelayCommand]
    private async Task RetryStartupAsync()
    {
        if (_retryAction is null)
            return;

        StartupErrorMessage = null;
        IsInitializing = true;
        await _retryAction();
    }
}
