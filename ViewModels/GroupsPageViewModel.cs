using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AttendanceJournal.ViewModels;
public partial class GroupsPageViewModel : ObservableObject
{
    private Func<Task>? _retryAction;

    public GroupsPageViewModel(GroupListViewModel groupList, StudentListViewModel studentList)
    {
        GroupList = groupList;
        StudentList = studentList;
        IsInitializing = true;

        GroupList.SelectedGroupIdChanged += id => _ = StudentList.LoadForGroupAsync(id);
    }

    public GroupListViewModel GroupList { get; }
    public StudentListViewModel StudentList { get; }

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