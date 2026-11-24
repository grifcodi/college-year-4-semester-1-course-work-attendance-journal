using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace AttendanceJournal.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISettingService _settingService;

    public MainViewModel(
        GroupsPageViewModel groupsPage,
        SettingsPageViewModel settingsPage,
        ISettingService settingService)
    {
        GroupsPage = groupsPage;
        SettingsPage = settingsPage;
        _settingService = settingService;
        CurrentPage = GroupsPage;

        LocalizationSource.Instance.PropertyChanged += OnLocalizationChanged;
    }

    // Texts of the window itself (title, navigation). They are plain properties that raise
    // PropertyChanged when the language changes, so they refresh without relying on the
    // indexer binding to LocalizationSource.
    public string AppTitleText => Localizer.Translate(UiTextKeys.AppTitle);
    public string NavHeaderText => Localizer.Translate(UiTextKeys.NavHeader);
    public string NavGroupsText => Localizer.Translate(UiTextKeys.NavGroups);
    public string NavSettingsText => Localizer.Translate(UiTextKeys.NavSettings);
    public string RetryText => Localizer.Translate(UiTextKeys.RetryButton);

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AppTitleText));
        OnPropertyChanged(nameof(NavHeaderText));
        OnPropertyChanged(nameof(NavGroupsText));
        OnPropertyChanged(nameof(NavSettingsText));
        OnPropertyChanged(nameof(RetryText));
    }

    public void Dispose() => LocalizationSource.Instance.PropertyChanged -= OnLocalizationChanged;

    public GroupsPageViewModel GroupsPage { get; }
    public SettingsPageViewModel SettingsPage { get; }

    [ObservableProperty]
    public partial ObservableObject? CurrentPage { get; set; }

    public async Task InitializeAsync()
    {
        var snapshot = await _settingService.GetSettingsAsync();
        SettingsPage.ApplySnapshot(snapshot);
        await GroupsPage.InitializeAsync();
    }

    public void SetStartupFailure(string localizedMessage, Func<Task> retryAction) =>
        GroupsPage.SetStartupFailure(localizedMessage, retryAction);

    [RelayCommand]
    private void NavigateToGroups() => CurrentPage = GroupsPage;

    [RelayCommand]
    private void NavigateToSettings() => CurrentPage = SettingsPage;
}


