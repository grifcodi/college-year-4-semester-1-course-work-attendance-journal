using AttendanceJournal.Models;

namespace AttendanceJournal.Services.Common;

public static class SettingMapper
{
    public static AppLanguage ParseLanguage(string? rawValue) =>
        Enum.TryParse<AppLanguage>(rawValue, ignoreCase: true, out var language)
            ? language
            : AppLanguage.Uk;

    public static string ToDbValue(AppLanguage language) => language.ToString();

    public static AppTheme ParseTheme(string? rawValue) =>
        Enum.TryParse<AppTheme>(rawValue, ignoreCase: true, out var theme)
            ? theme
            : AppTheme.Light;

    public static string ToDbValue(AppTheme theme) => theme.ToString();
}