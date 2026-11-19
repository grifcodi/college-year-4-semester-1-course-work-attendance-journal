using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AttendanceJournal.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(GroupsPageViewModel groupsPage)
    {
        GroupsPage = groupsPage;
        CurrentPage = GroupsPage;
    }

    public GroupsPageViewModel GroupsPage { get; }

    [ObservableProperty]
    public partial ObservableObject? CurrentPage { get; set; }

    public Task InitializeAsync() => GroupsPage.InitializeAsync();

    public void SetStartupFailure(string localizedMessage, Func<Task> retryAction) =>
        GroupsPage.SetStartupFailure(localizedMessage, retryAction);

    [RelayCommand]
    private void NavigateToGroups() => CurrentPage = GroupsPage;
}