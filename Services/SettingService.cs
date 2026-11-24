using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class SettingService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), ISettingService
{
    public const string LanguageKey = "Language";
    public const string ThemeKey = "Theme";

    public async Task<AppSettingsSnapshot> GetSettingsAsync()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var values = await context.Settings
            .AsNoTracking()
            .Where(s => s.Key == LanguageKey || s.Key == ThemeKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var language = SettingMapper.ParseLanguage(values.GetValueOrDefault(LanguageKey));
        var theme = SettingMapper.ParseTheme(values.GetValueOrDefault(ThemeKey));

        return new AppSettingsSnapshot(language, theme);
    }

    public Task<Result> SetLanguageAsync(AppLanguage language) =>
        UpsertSettingAsync(LanguageKey, SettingMapper.ToDbValue(language));

    public Task<Result> SetThemeAsync(AppTheme theme) =>
        UpsertSettingAsync(ThemeKey, SettingMapper.ToDbValue(theme));

    private Task<Result> UpsertSettingAsync(string key, string newValue)
    {
        return ExecuteInTransactionAsync(async context =>
        {
            var setting = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting is not null && string.Equals(setting.Value, newValue, StringComparison.Ordinal))
                return Result.Success();

            var oldValue = setting?.Value;

            if (setting is null)
            {
                setting = new Setting { Key = key, Value = newValue };
                context.Settings.Add(setting);
            }
            else
            {
                setting.Value = newValue;
            }

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.Setting,
                entityId: null,
                new { Key = key, OldValue = oldValue, NewValue = newValue }));

            await context.SaveChangesAsync();
            return Result.Success();
        });
    }
}