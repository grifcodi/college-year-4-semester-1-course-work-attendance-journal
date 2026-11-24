using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;

namespace AttendanceJournal.Tests;

[Collection(AttendanceJournal.Tests.TestHelpers.LocalizerCollection.Name)]
public class T16_GroupListViewModelTests : SqliteInMemoryTestBase
{
    private readonly FakeConfirmationService _confirm = new();
    private readonly FakeErrorLog _log = new();

    private StudentGroupService Service => new(ContextFactory);

    private GroupListViewModel CreateVm() => new(Service, _confirm, _log);

    private static string Uk(string key, params object[] args) =>
        Localizer.TranslateError(Result.Failure(key, args), AppLanguage.Uk);

    [Fact]
    public async Task Load_ShowsGroups_AndSelectsFirst()
    {
        await Service.CreateAsync("Б-1", "2025-2026");
        await Service.CreateAsync("А-1", "2026-2027");
        var vm = CreateVm();

        await vm.LoadAsync();

        Assert.Equal(["А-1", "Б-1"], vm.Groups.Select(g => g.Name));
        Assert.Equal("А-1", vm.SelectedGroup!.Name);
    }

    [Fact]
    public async Task Save_NewGroup_AddsToList_AndClearsForm()
    {
        var vm = CreateVm();
        vm.GroupNameInput = "ІПЗ-21";
        vm.AcademicYearInput = "2026-2027";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("ІПЗ-21", Assert.Single(vm.Groups).Name);
        Assert.Equal(string.Empty, vm.GroupNameInput);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task Save_EmptyName_ShowsLocalizedError_AndKeepsForm()
    {
        var vm = CreateVm();
        vm.GroupNameInput = "";
        vm.AcademicYearInput = "2026-2027";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Uk(ErrorKeys.GroupNameRequired), vm.ErrorMessage);
        Assert.Empty(vm.Groups);
        Assert.Equal("2026-2027", vm.AcademicYearInput);
    }

    [Fact]
    public async Task Save_DuplicateName_ShowsError()
    {
        await Service.CreateAsync("ІПЗ-21", "2026-2027");
        var vm = CreateVm();
        await vm.LoadAsync();
        vm.GroupNameInput = "іпз-21";
        vm.AcademicYearInput = "2026-2027";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Uk(ErrorKeys.GroupAlreadyExists, "іпз-21"), vm.ErrorMessage);
        Assert.Single(vm.Groups);
    }

    [Fact]
    public async Task StartEdit_FillsForm_AndSaveUpdatesGroup()
    {
        await Service.CreateAsync("ІПЗ-21", "2026-2027");
        var vm = CreateVm();
        await vm.LoadAsync();

        vm.StartEditCommand.Execute(null);
        Assert.True(vm.IsEditing);
        Assert.Equal("ІПЗ-21", vm.GroupNameInput);

        vm.GroupNameInput = "ІПЗ-22";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsEditing);
        Assert.Equal("ІПЗ-22", Assert.Single(vm.Groups).Name);
    }

    [Fact]
    public async Task CancelEdit_ResetsForm()
    {
        await Service.CreateAsync("ІПЗ-21", "2026-2027");
        var vm = CreateVm();
        await vm.LoadAsync();
        vm.StartEditCommand.Execute(null);

        vm.CancelEditCommand.Execute(null);

        Assert.False(vm.IsEditing);
        Assert.Equal(string.Empty, vm.GroupNameInput);
    }

    [Fact]
    public async Task Archive_AsksForConfirmation_ThenArchives()
    {
        await Service.CreateAsync("ІПЗ-21", "2026-2027");
        await Service.CreateAsync("ІПЗ-22", "2026-2027");
        var vm = CreateVm();
        await vm.LoadAsync();
        var target = vm.Groups.First(g => g.Name == "ІПЗ-21");

        await vm.ToggleArchiveCommand.ExecuteAsync(target);

        Assert.Contains("ІПЗ-21", Assert.Single(_confirm.Messages));
        Assert.Equal(["ІПЗ-22"], vm.Groups.Select(g => g.Name));
    }

    [Fact]
    public async Task Archive_WhenDeclined_ChangesNothing()
    {
        _confirm.Answer = false;
        await Service.CreateAsync("ІПЗ-21", "2026-2027");
        var vm = CreateVm();
        await vm.LoadAsync();

        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Groups[0]);

        Assert.False(Assert.Single(vm.Groups).IsArchived);
    }

    [Fact]
    public async Task Unarchive_DoesNotAskForConfirmation()
    {
        var created = await Service.CreateAsync("ІПЗ-21", "2026-2027");
        await Service.ArchiveAsync(created.Value!.StudentGroupId);
        var vm = CreateVm();
        vm.ShowArchived = true;
        await TestWait.UntilAsync(() => vm.Groups.Count == 1);

        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Groups[0]);

        Assert.Empty(_confirm.Messages);
        Assert.False(Assert.Single(vm.Groups).IsArchived);
    }

    [Fact]
    public async Task ShowArchived_ReloadsList()
    {
        var created = await Service.CreateAsync("ІПЗ-21", "2026-2027");
        await Service.CreateAsync("ІПЗ-22", "2026-2027");
        await Service.ArchiveAsync(created.Value!.StudentGroupId);
        var vm = CreateVm();
        await vm.LoadAsync();
        Assert.Single(vm.Groups);

        vm.ShowArchived = true;

        await TestWait.UntilAsync(() => vm.Groups.Count == 2);
    }

    [Fact]
    public async Task SelectionChange_RaisesSelectedGroupIdChanged()
    {
        await Service.CreateAsync("А-1", "2026-2027");
        await Service.CreateAsync("Б-1", "2026-2027");
        var vm = CreateVm();
        var ids = new List<int?>();
        vm.SelectedGroupIdChanged += ids.Add;

        await vm.LoadAsync();

        Assert.Equal(vm.Groups[0].StudentGroupId, ids.Last());
    }

    [Fact]
    public async Task ServiceException_ShowsUnexpectedError_AndWritesToLog()
    {
        var vm = new GroupListViewModel(
            new StudentGroupService(new ThrowingContextFactory()), _confirm, _log);

        await vm.LoadAsync();

        Assert.Equal(Uk(ErrorKeys.UnexpectedError), vm.ErrorMessage);
        var entry = Assert.Single(_log.Entries);
        Assert.Equal("GroupList.Load", entry.Context);
    }

    [Fact]
    public async Task LoadCore_PropagatesException_ForStartupHandling()
    {
        var vm = new GroupListViewModel(
            new StudentGroupService(new ThrowingContextFactory()), _confirm, _log);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.LoadCoreAsync());
    }
}


