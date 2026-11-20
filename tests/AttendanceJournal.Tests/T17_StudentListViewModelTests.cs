using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;

namespace AttendanceJournal.Tests;

public class T17_StudentListViewModelTests : SqliteInMemoryTestBase
{
    private readonly FakeConfirmationService _confirm = new();
    private readonly FakeErrorLog _log = new();

    private StudentGroupService Groups => new(ContextFactory);
    private StudentService Students => new(ContextFactory);

    private StudentListViewModel CreateVm() => new(Students, _confirm, _log);

    private async Task<int> CreateGroupAsync(string name = "ІПЗ-21") =>
        (await Groups.CreateAsync(name, "2026-2027")).Value!.StudentGroupId;

    private static string Uk(string key, params object[] args) =>
        Localizer.TranslateError(Result.Failure(key, args), AppLanguage.Uk);

    [Fact]
    public async Task LoadForGroup_ShowsOnlyThatGroupsStudents()
    {
        var g1 = await CreateGroupAsync("ІПЗ-21");
        var g2 = await CreateGroupAsync("ІПЗ-22");
        await Students.AddAsync(g1, "Бойко Олена");
        await Students.AddAsync(g2, "Франко Іван");
        var vm = CreateVm();

        await vm.LoadForGroupAsync(g1);

        Assert.Equal(["Бойко Олена"], vm.Students.Select(s => s.FullName));
        Assert.True(vm.HasGroup);
    }

    [Fact]
    public async Task LoadForGroup_Null_ClearsListAndDisablesForm()
    {
        var g1 = await CreateGroupAsync();
        await Students.AddAsync(g1, "Бойко Олена");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);

        await vm.LoadForGroupAsync(null);

        Assert.Empty(vm.Students);
        Assert.False(vm.HasGroup);
    }

    [Fact]
    public async Task Save_AddsStudentToCurrentGroup()
    {
        var g1 = await CreateGroupAsync();
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);
        vm.FullNameInput = "Шевченко Тарас";

        await vm.SaveCommand.ExecuteAsync(null);

        var student = Assert.Single(vm.Students);
        Assert.Equal("Шевченко Тарас", student.FullName);
        Assert.Equal(g1, student.StudentGroupId);
        Assert.Equal(string.Empty, vm.FullNameInput);
    }

    [Fact]
    public async Task Save_WithoutGroup_DoesNothing()
    {
        var vm = CreateVm();
        vm.FullNameInput = "Шевченко Тарас";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(vm.Students);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task Save_DuplicateName_ShowsError()
    {
        var g1 = await CreateGroupAsync();
        await Students.AddAsync(g1, "Шевченко Тарас");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);
        vm.FullNameInput = "ШЕВЧЕНКО ТАРАС";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Uk(ErrorKeys.StudentAlreadyExists, "ШЕВЧЕНКО ТАРАС"), vm.ErrorMessage);
        Assert.Single(vm.Students);
    }

    [Fact]
    public async Task Edit_UpdatesTheChosenStudent()
    {
        var g1 = await CreateGroupAsync();
        await Students.AddAsync(g1, "Шевченко Тарас");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);

        vm.StartEditCommand.Execute(vm.Students[0]);
        Assert.True(vm.IsEditing);
        Assert.Equal("Шевченко Тарас", vm.FullNameInput);

        vm.FullNameInput = "Франко Іван";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsEditing);
        Assert.Equal("Франко Іван", Assert.Single(vm.Students).FullName);
    }

    [Fact]
    public async Task ChangingGroup_CancelsEdit()
    {
        var g1 = await CreateGroupAsync("ІПЗ-21");
        var g2 = await CreateGroupAsync("ІПЗ-22");
        await Students.AddAsync(g1, "Шевченко Тарас");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);
        vm.StartEditCommand.Execute(vm.Students[0]);

        await vm.LoadForGroupAsync(g2);

        Assert.False(vm.IsEditing);
        Assert.Equal(string.Empty, vm.FullNameInput);
    }

    [Fact]
    public async Task Archive_AsksForConfirmation_ThenHidesStudent()
    {
        var g1 = await CreateGroupAsync();
        await Students.AddAsync(g1, "Шевченко Тарас");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);

        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Students[0]);

        Assert.Contains("Шевченко Тарас", Assert.Single(_confirm.Messages));
        Assert.Empty(vm.Students);
    }

    [Fact]
    public async Task Archive_WhenDeclined_ChangesNothing()
    {
        _confirm.Answer = false;
        var g1 = await CreateGroupAsync();
        await Students.AddAsync(g1, "Шевченко Тарас");
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);

        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Students[0]);

        Assert.Single(vm.Students);
    }

    [Fact]
    public async Task ShowArchived_IncludesArchivedStudents_AndUnarchiveNeedsNoConfirmation()
    {
        var g1 = await CreateGroupAsync();
        var student = await Students.AddAsync(g1, "Шевченко Тарас");
        await Students.ArchiveAsync(student.Value!.StudentId);
        var vm = CreateVm();
        await vm.LoadForGroupAsync(g1);
        Assert.Empty(vm.Students);

        vm.ShowArchived = true;
        await TestWait.UntilAsync(() => vm.Students.Count == 1);
        await vm.ToggleArchiveCommand.ExecuteAsync(vm.Students[0]);

        Assert.Empty(_confirm.Messages);
        Assert.False(Assert.Single(vm.Students).IsArchived);
    }

    [Fact]
    public async Task ServiceException_ShowsUnexpectedError_AndWritesToLog()
    {
        var vm = new StudentListViewModel(
            new StudentService(new ThrowingContextFactory()), _confirm, _log);

        await vm.LoadForGroupAsync(1);

        Assert.Equal(Uk(ErrorKeys.UnexpectedError), vm.ErrorMessage);
        Assert.Equal("StudentList.Load", Assert.Single(_log.Entries).Context);
    }
}