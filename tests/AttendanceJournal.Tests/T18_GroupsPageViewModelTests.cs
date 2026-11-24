using AttendanceJournal.Services;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;

namespace AttendanceJournal.Tests;

[Collection(AttendanceJournal.Tests.TestHelpers.LocalizerCollection.Name)]
public class T18_GroupsPageViewModelTests : SqliteInMemoryTestBase
{
    private readonly FakeConfirmationService _confirm = new();
    private readonly FakeErrorLog _log = new();

    private GroupsPageViewModel CreatePage(bool failingDatabase = false)
    {
        var groups = new StudentGroupService(failingDatabase ? new ThrowingContextFactory() : ContextFactory);
        var students = new StudentService(failingDatabase ? new ThrowingContextFactory() : ContextFactory);

        var import = new StudentImportService(ContextFactory, new StudentFileReader());

        return new GroupsPageViewModel(
            new GroupListViewModel(groups, _confirm, _log),
            new StudentListViewModel(students, _confirm, _log),
            new StudentImportViewModel(import, new FakeFileDialogService(), _confirm, _log));
    }

    [Fact]
    public async Task Initialize_LoadsGroups_AndStopsInitializing()
    {
        await new StudentGroupService(ContextFactory).CreateAsync("ІПЗ-21", "2026-2027");
        var page = CreatePage();
        Assert.True(page.IsInitializing);

        await page.InitializeAsync();

        Assert.False(page.IsInitializing);
        Assert.Single(page.GroupList.Groups);
    }

    [Fact]
    public async Task SelectingGroup_LoadsItsStudents()
    {
        var groups = new StudentGroupService(ContextFactory);
        var g1 = (await groups.CreateAsync("А-1", "2026-2027")).Value!;
        var g2 = (await groups.CreateAsync("Б-1", "2026-2027")).Value!;
        var students = new StudentService(ContextFactory);
        await students.AddAsync(g1.StudentGroupId, "Бойко Олена");
        await students.AddAsync(g2.StudentGroupId, "Франко Іван");
        var page = CreatePage();
        await page.InitializeAsync();
        await TestWait.UntilAsync(() => page.StudentList.Students.Count == 1);
        Assert.Equal("Бойко Олена", page.StudentList.Students[0].FullName);

        page.GroupList.SelectedGroup = page.GroupList.Groups.First(g => g.Name == "Б-1");

        await TestWait.UntilAsync(() =>
            page.StudentList.Students.Count == 1 && page.StudentList.Students[0].FullName == "Франко Іван");
    }

    [Fact]
    public async Task Initialize_WhenDatabaseFails_ThrowsAndStopsInitializing()
    {
        var page = CreatePage(failingDatabase: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => page.InitializeAsync());

        Assert.False(page.IsInitializing);
    }

    [Fact]
    public void SetStartupFailure_ShowsMessage_AndStopsInitializing()
    {
        var page = CreatePage();

        page.SetStartupFailure("Помилка запуску", () => Task.CompletedTask);

        Assert.False(page.IsInitializing);
        Assert.Equal("Помилка запуску", page.StartupErrorMessage);
    }

    [Fact]
    public async Task Retry_ClearsMessage_ShowsBusy_AndRunsRetryAction()
    {
        var page = CreatePage();
        var retried = 0;
        page.SetStartupFailure("Помилка запуску", () => { retried++; return Task.CompletedTask; });

        await page.RetryStartupCommand.ExecuteAsync(null);

        Assert.Equal(1, retried);
        Assert.Null(page.StartupErrorMessage);
        Assert.True(page.IsInitializing);
    }

    [Fact]
    public async Task Retry_WithoutFailure_DoesNothing()
    {
        var page = CreatePage();

        await page.RetryStartupCommand.ExecuteAsync(null);

        Assert.True(page.IsInitializing);
        Assert.Null(page.StartupErrorMessage);
    }
}
