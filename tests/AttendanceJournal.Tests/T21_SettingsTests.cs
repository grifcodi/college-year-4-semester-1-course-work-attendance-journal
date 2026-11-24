using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;

namespace AttendanceJournal.Tests;

[Collection(AttendanceJournal.Tests.TestHelpers.LocalizerCollection.Name)]
public class T21_SettingsTests : SqliteInMemoryTestBase
{
    private sealed class FakeThemeApplier : IThemeApplier
    {
        public List<AppTheme> AppliedThemes { get; } = [];
        public void Apply(AppTheme theme) => AppliedThemes.Add(theme);
    }

    private SettingService CreateService() => new(ContextFactory);

    [Fact]
    public async Task GetSettings_ReturnsSeededDefaults_UkAndLight()
    {
        var snapshot = await CreateService().GetSettingsAsync();

        Assert.Equal(AppLanguage.Uk, snapshot.Language);
        Assert.Equal(AppTheme.Light, snapshot.Theme);
    }

    [Fact]
    public async Task SetLanguageAndTheme_PersistsValues_AndWritesAuditEntries()
    {
        var service = CreateService();

        var langResult = await service.SetLanguageAsync(AppLanguage.En);
        var themeResult = await service.SetThemeAsync(AppTheme.Dark);

        Assert.True(langResult.IsSuccess);
        Assert.True(themeResult.IsSuccess);

        var snapshot = await service.GetSettingsAsync();
        Assert.Equal(AppLanguage.En, snapshot.Language);
        Assert.Equal(AppTheme.Dark, snapshot.Theme);

        var audit = await ContextFactory.GetAuditAsync(AuditEntityType.Setting);
        Assert.Equal(2, audit.Count);
        Assert.All(audit, e => Assert.Equal(AuditAction.Update, e.Action));
    }

    [Fact]
    public async Task SetSameValue_DoesNotWriteDuplicateAuditEntry()
    {
        var service = CreateService();
        var before = (await ContextFactory.GetAuditAsync(AuditEntityType.Setting)).Count;

        await service.SetLanguageAsync(AppLanguage.Uk);
        await service.SetThemeAsync(AppTheme.Light);

        Assert.Equal(before, (await ContextFactory.GetAuditAsync(AuditEntityType.Setting)).Count);
    }

    [Fact]
    public async Task SettingsPageViewModel_AppliesSnapshotAndPersistsChanges()
    {
        var originalLang = Localizer.CurrentLanguage;
        try
        {
            var service = CreateService();
            var themeApplier = new FakeThemeApplier();
            var vm = new SettingsPageViewModel(service, themeApplier, new FakeErrorLog());

            vm.ApplySnapshot(new AppSettingsSnapshot(AppLanguage.En, AppTheme.Dark));
            Assert.Equal(AppLanguage.En, Localizer.CurrentLanguage);
            Assert.Equal(AppTheme.Dark, themeApplier.AppliedThemes.Last());

            await vm.PersistLanguageAsync(AppLanguage.Uk);
            await vm.PersistThemeAsync(AppTheme.Light);

            Assert.Equal(AppLanguage.Uk, Localizer.CurrentLanguage);
            Assert.Equal(AppTheme.Light, themeApplier.AppliedThemes.Last());
            var saved = await service.GetSettingsAsync();
            Assert.Equal(AppLanguage.Uk, saved.Language);
            Assert.Equal(AppTheme.Light, saved.Theme);
        }
        finally
        {
            Localizer.CurrentLanguage = originalLang;
        }
    }
}


