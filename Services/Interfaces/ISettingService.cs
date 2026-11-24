using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public record AppSettingsSnapshot(AppLanguage Language, AppTheme Theme);

public interface ISettingService
{
    Task<AppSettingsSnapshot> GetSettingsAsync();
    Task<Result> SetLanguageAsync(AppLanguage language);
    Task<Result> SetThemeAsync(AppTheme theme);
}