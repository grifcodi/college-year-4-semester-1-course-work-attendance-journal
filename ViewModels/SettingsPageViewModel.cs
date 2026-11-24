using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AttendanceJournal.ViewModels;

public partial class SettingsPageViewModel(
    ISettingService settingService,
    IThemeApplier themeApplier,
    IErrorLog errorLog) : OperationViewModelBase(errorLog)
{
    // Persist requests run one at a time; a request that was superseded by a newer click is skipped.
    private readonly SemaphoreSlim _persistLock = new(1, 1);
    private int _languageRequest;
    private int _themeRequest;

    // What is really saved and applied. The radio buttons are reset to these if saving fails.
    private AppLanguage _savedLanguage = AppLanguage.Uk;
    private AppTheme _savedTheme = AppTheme.Light;

    private bool _isSyncing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUkrainianSelected))]
    [NotifyPropertyChangedFor(nameof(IsEnglishSelected))]
    public partial AppLanguage SelectedLanguage { get; set; } = Localizer.CurrentLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightThemeSelected))]
    [NotifyPropertyChangedFor(nameof(IsDarkThemeSelected))]
    public partial AppTheme SelectedTheme { get; set; } = AppTheme.Light;

    public bool IsUkrainianSelected
    {
        get => SelectedLanguage == AppLanguage.Uk;
        set { if (value) SelectedLanguage = AppLanguage.Uk; }
    }

    public bool IsEnglishSelected
    {
        get => SelectedLanguage == AppLanguage.En;
        set { if (value) SelectedLanguage = AppLanguage.En; }
    }

    public bool IsLightThemeSelected
    {
        get => SelectedTheme == AppTheme.Light;
        set { if (value) SelectedTheme = AppTheme.Light; }
    }

    public bool IsDarkThemeSelected
    {
        get => SelectedTheme == AppTheme.Dark;
        set { if (value) SelectedTheme = AppTheme.Dark; }
    }

    public void ApplySnapshot(AppSettingsSnapshot snapshot)
    {
        _savedLanguage = snapshot.Language;
        _savedTheme = snapshot.Theme;

        _isSyncing = true;
        try
        {
            SelectedLanguage = snapshot.Language;
            SelectedTheme = snapshot.Theme;
            Localizer.CurrentLanguage = snapshot.Language;
            themeApplier.Apply(snapshot.Theme);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnSelectedLanguageChanged(AppLanguage value)
    {
        if (_isSyncing)
            return;

        _ = PersistLanguageAsync(value);
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        if (_isSyncing)
            return;

        _ = PersistThemeAsync(value);
    }

    public Task PersistLanguageAsync(AppLanguage language)
    {
        var request = ++_languageRequest;

        return RunSafeAsync("Settings.Language", async () =>
        {
            await _persistLock.WaitAsync();
            try
            {
                if (request != _languageRequest)
                    return;

                ClearError();
                var saved = false;
                try
                {
                    var result = await settingService.SetLanguageAsync(language);
                    if (!result.IsSuccess)
                    {
                        ErrorMessage = Localizer.TranslateError(result);
                        return;
                    }

                    _savedLanguage = language;
                    Localizer.CurrentLanguage = language;
                    saved = true;
                }
                finally
                {
                    if (request == _languageRequest)
                        SetLanguageSilently(saved ? language : _savedLanguage);
                }
            }
            finally
            {
                _persistLock.Release();
            }
        }, allowRetry: true);
    }

    public Task PersistThemeAsync(AppTheme theme)
    {
        var request = ++_themeRequest;

        return RunSafeAsync("Settings.Theme", async () =>
        {
            await _persistLock.WaitAsync();
            try
            {
                if (request != _themeRequest)
                    return;

                ClearError();
                var saved = false;
                try
                {
                    var result = await settingService.SetThemeAsync(theme);
                    if (!result.IsSuccess)
                    {
                        ErrorMessage = Localizer.TranslateError(result);
                        return;
                    }

                    _savedTheme = theme;
                    themeApplier.Apply(theme);
                    saved = true;
                }
                finally
                {
                    if (request == _themeRequest)
                        SetThemeSilently(saved ? theme : _savedTheme);
                }
            }
            finally
            {
                _persistLock.Release();
            }
        }, allowRetry: true);
    }

    private void SetLanguageSilently(AppLanguage value)
    {
        _isSyncing = true;
        try { SelectedLanguage = value; }
        finally { _isSyncing = false; }
    }

    private void SetThemeSilently(AppTheme value)
    {
        _isSyncing = true;
        try { SelectedTheme = value; }
        finally { _isSyncing = false; }
    }
}
