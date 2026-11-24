using AttendanceJournal.Models;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceJournal.Tests;

[Collection(LocalizerCollection.Name)]
public class T23_MainViewModelLocalizationTests
{
    [Fact]
    public void NavigationTexts_FollowLanguage_AndRaisePropertyChanged()
    {
        var original = Localizer.CurrentLanguage;
        try
        {
            Localizer.CurrentLanguage = AppLanguage.Uk;
            using var provider = new ServiceCollection()
                .AddAttendanceJournal("Data Source=:memory:")
                .BuildServiceProvider();
            var vm = provider.GetRequiredService<MainViewModel>();
            var changed = new List<string?>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            Assert.Equal("Групи та студенти", vm.NavGroupsText);

            Localizer.CurrentLanguage = AppLanguage.En;

            Assert.Equal("Groups & Students", vm.NavGroupsText);
            Assert.Equal("Settings", vm.NavSettingsText);
            Assert.Equal("Journal", vm.NavHeaderText);
            Assert.Equal("Electronic Attendance Journal", vm.AppTitleText);
            Assert.Contains(nameof(MainViewModel.NavGroupsText), changed);
            Assert.Contains(nameof(MainViewModel.NavHeaderText), changed);
            Assert.Contains(nameof(MainViewModel.NavSettingsText), changed);
        }
        finally
        {
            Localizer.CurrentLanguage = original;
        }
    }
}
