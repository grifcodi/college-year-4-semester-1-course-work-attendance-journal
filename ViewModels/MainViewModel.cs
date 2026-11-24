using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
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
        SubjectsPageViewModel subjectsPage,
        JournalPageViewModel journalPage,
        MonthGridPageViewModel monthGridPage,
        SummaryPageViewModel summaryPage,
        AuditLogPageViewModel auditLogPage,
        SettingsPageViewModel settingsPage,
        ISettingService settingService)
    {
        GroupsPage = groupsPage;
        SubjectsPage = subjectsPage;
        JournalPage = journalPage;
        MonthGridPage = monthGridPage;
        SummaryPage = summaryPage;
        AuditLogPage = auditLogPage;
        SettingsPage = settingsPage;
        _settingService = settingService;
        CurrentPage = GroupsPage;

        MonthGridPage.OpenLessonRequested += OnOpenLessonRequested;
        LocalizationSource.Instance.PropertyChanged += OnLocalizationChanged;
    }

    // Texts of the window itself (title, navigation). They are plain properties that raise
    // PropertyChanged when the language changes, so they refresh without relying on the
    // indexer binding to LocalizationSource.
    public string AppTitleText => Localizer.Translate(UiTextKeys.AppTitle);
    public string NavHeaderText => Localizer.Translate(UiTextKeys.NavHeader);
    public string NavGroupsText => Localizer.Translate(UiTextKeys.NavGroups);
    public string NavSubjectsText => Localizer.Translate(UiTextKeys.NavSubjects);
    public string NavJournalText => Localizer.Translate(UiTextKeys.NavJournal);
    public string NavMonthGridText => Localizer.Translate(UiTextKeys.NavMonthGrid);
    public string NavSummaryText => Localizer.Translate(UiTextKeys.NavSummary);
    public string NavAuditLogText => Localizer.Translate(UiTextKeys.NavAuditLog);
    public string NavSettingsText => Localizer.Translate(UiTextKeys.NavSettings);
    public string RetryText => Localizer.Translate(UiTextKeys.RetryButton);

    private static readonly string[] ShellTextProperties =
    [
        nameof(AppTitleText), nameof(NavHeaderText), nameof(NavGroupsText), nameof(NavSubjectsText),
        nameof(NavJournalText), nameof(NavMonthGridText), nameof(NavSummaryText), nameof(NavAuditLogText),
        nameof(NavSettingsText), nameof(RetryText),
    ];

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var name in ShellTextProperties)
            OnPropertyChanged(name);
    }

    public void Dispose()
    {
        LocalizationSource.Instance.PropertyChanged -= OnLocalizationChanged;
        MonthGridPage.OpenLessonRequested -= OnOpenLessonRequested;
    }

    public GroupsPageViewModel GroupsPage { get; }
    public SubjectsPageViewModel SubjectsPage { get; }
    public JournalPageViewModel JournalPage { get; }
    public MonthGridPageViewModel MonthGridPage { get; }
    public SummaryPageViewModel SummaryPage { get; }
    public AuditLogPageViewModel AuditLogPage { get; }
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

    private Task NavigateToAsync(ObservableObject page)
    {
        CurrentPage = page;
        return page is IActivatablePage activatable ? activatable.ActivateAsync() : Task.CompletedTask;
    }

    private async void OnOpenLessonRequested(int lessonId)
    {
        // Show the journal without ActivateAsync: OpenLessonAsync loads the pickers itself.
        // async void is acceptable for an event handler; OpenLessonAsync catches its own errors.
        CurrentPage = JournalPage;
        await JournalPage.OpenLessonAsync(lessonId);
    }

    [RelayCommand]
    private Task NavigateToGroupsAsync() => NavigateToAsync(GroupsPage);

    [RelayCommand]
    private Task NavigateToSubjectsAsync() => NavigateToAsync(SubjectsPage);

    [RelayCommand]
    private Task NavigateToJournalAsync() => NavigateToAsync(JournalPage);

    [RelayCommand]
    private Task NavigateToMonthGridAsync() => NavigateToAsync(MonthGridPage);

    [RelayCommand]
    private Task NavigateToSummaryAsync() => NavigateToAsync(SummaryPage);

    [RelayCommand]
    private Task NavigateToAuditLogAsync() => NavigateToAsync(AuditLogPage);

    [RelayCommand]
    private Task NavigateToSettingsAsync() => NavigateToAsync(SettingsPage);
}
