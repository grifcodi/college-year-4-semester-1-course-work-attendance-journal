using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T02_StudentTests : SqliteInMemoryTestBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private StudentService CreateService() => new(ContextFactory);

    private async Task<int> CreateGroupAsync(string name = "ІПЗ-21")
    {
        var group = await new StudentGroupService(ContextFactory).CreateAsync(name, "2026-2027");
        return group.Value!.StudentGroupId;
    }

    private async Task<List<Student>> AllStudentsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.Students.AsNoTracking().OrderBy(s => s.StudentId).ToListAsync(Ct);
    }

    [Fact]
    public async Task Add_StoresTrimmedNameInGroup()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();

        var result = await service.AddAsync(groupId, "  Шевченко Тарас  ");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await AllStudentsAsync());
        Assert.Equal("Шевченко Тарас", saved.FullName);
        Assert.Equal(groupId, saved.StudentGroupId);
        Assert.False(saved.IsArchived);
    }

    [Fact]
    public async Task Add_ToMissingGroup_ReturnsEntityNotFound()
    {
        var result = await CreateService().AddAsync(999, "Шевченко Тарас");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
        Assert.Empty(await AllStudentsAsync());
    }

    [Fact]
    public async Task Add_DuplicateNameInSameGroup_IsRejectedIgnoringCase()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        await service.AddAsync(groupId, "Шевченко Тарас");

        var duplicate = await service.AddAsync(groupId, "ШЕВЧЕНКО ТАРАС");

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ErrorKeys.StudentAlreadyExists, duplicate.ErrorKey);
        Assert.Single(await AllStudentsAsync());
    }

    [Fact]
    public async Task Add_SameNameInDifferentGroup_IsAllowed()
    {
        var service = CreateService();
        var group1 = await CreateGroupAsync("ІПЗ-21");
        var group2 = await CreateGroupAsync("ІПЗ-22");
        await service.AddAsync(group1, "Шевченко Тарас");

        var result = await service.AddAsync(group2, "Шевченко Тарас");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, (await AllStudentsAsync()).Count);
    }

    [Fact]
    public async Task Add_NameOfArchivedStudent_IsRejected()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        var student = await service.AddAsync(groupId, "Шевченко Тарас");
        await service.ArchiveAsync(student.Value!.StudentId);

        var again = await service.AddAsync(groupId, "Шевченко Тарас");

        Assert.False(again.IsSuccess);
        Assert.Equal(ErrorKeys.StudentAlreadyExists, again.ErrorKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_EmptyName_IsRejected(string name)
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();

        var result = await service.AddAsync(groupId, name);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.StudentNameRequired, result.ErrorKey);
    }

    [Fact]
    public async Task Add_Name150Chars_IsAccepted_And151IsRejected()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();

        var ok = await service.AddAsync(groupId, new string('а', 150));
        var tooLong = await service.AddAsync(groupId, new string('б', 151));

        Assert.True(ok.IsSuccess);
        Assert.Equal(ErrorKeys.StudentNameTooLong, tooLong.ErrorKey);
    }

    [Fact]
    public async Task Update_ChangesFullName()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        var student = await service.AddAsync(groupId, "Шевченко Тарас");

        var result = await service.UpdateAsync(student.Value!.StudentId, "Франко Іван");

        Assert.True(result.IsSuccess);
        Assert.Equal("Франко Іван", Assert.Single(await AllStudentsAsync()).FullName);
    }

    [Fact]
    public async Task Update_ToNameOfClassmate_IsRejected()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        await service.AddAsync(groupId, "Шевченко Тарас");
        var second = await service.AddAsync(groupId, "Франко Іван");

        var result = await service.UpdateAsync(second.Value!.StudentId, "шевченко тарас");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.StudentAlreadyExists, result.ErrorKey);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsEntityNotFound()
    {
        var result = await CreateService().UpdateAsync(999, "Франко Іван");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task Archive_KeepsRow_AndHidesFromDefaultList()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        var student = await service.AddAsync(groupId, "Шевченко Тарас");

        var result = await service.ArchiveAsync(student.Value!.StudentId);

        Assert.True(result.IsSuccess);
        Assert.Empty(await service.GetByGroupAsync(groupId, includeArchived: false));
        var all = await service.GetByGroupAsync(groupId, includeArchived: true);
        Assert.True(Assert.Single(all).IsArchived);
    }

    [Fact]
    public async Task Unarchive_ReturnsStudentToDefaultList()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        var student = await service.AddAsync(groupId, "Шевченко Тарас");
        await service.ArchiveAsync(student.Value!.StudentId);

        var result = await service.UnarchiveAsync(student.Value.StudentId);

        Assert.True(result.IsSuccess);
        Assert.Single(await service.GetByGroupAsync(groupId));
    }

    [Fact]
    public async Task ArchiveGroup_DoesNotChangeItsStudents()
    {
        var service = CreateService();
        var groupId = await CreateGroupAsync();
        await service.AddAsync(groupId, "Шевченко Тарас");

        await new StudentGroupService(ContextFactory).ArchiveAsync(groupId);

        var students = await service.GetByGroupAsync(groupId, includeArchived: true);
        var student = Assert.Single(students);
        Assert.False(student.IsArchived);
    }

    [Fact]
    public async Task Archive_NotFound_ReturnsEntityNotFound()
    {
        var result = await CreateService().ArchiveAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task GetStudentsByGroup_ReturnsOnlyThatGroup_OrderedByName()
    {
        var service = CreateService();
        var group1 = await CreateGroupAsync("ІПЗ-21");
        var group2 = await CreateGroupAsync("ІПЗ-22");
        await service.AddAsync(group1, "Франко Іван");
        await service.AddAsync(group1, "Бойко Олена");
        await service.AddAsync(group2, "Інший Студент");

        var list = await service.GetByGroupAsync(group1);

        Assert.Equal(["Бойко Олена", "Франко Іван"], list.Select(s => s.FullName));
    }
}