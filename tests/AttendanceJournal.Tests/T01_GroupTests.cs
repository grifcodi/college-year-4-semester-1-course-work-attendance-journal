using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T01_GroupTests : SqliteInMemoryTestBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private StudentGroupService CreateService() => new(ContextFactory);

    private async Task<List<StudentGroup>> AllGroupsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.StudentGroups.AsNoTracking().OrderBy(g => g.StudentGroupId).ToListAsync(Ct);
    }

    [Fact]
    public async Task Create_StoresNameAndAcademicYear()
    {
        var result = await CreateService().CreateAsync("  ІПЗ-21  ", " 2026-2027 ");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await AllGroupsAsync());
        Assert.Equal("ІПЗ-21", saved.Name);
        Assert.Equal("ІПЗ-21", saved.NormalizedName);
        Assert.Equal("2026-2027", saved.AcademicYear);
        Assert.False(saved.IsArchived);
    }

    [Fact]
    public async Task Create_DuplicateActiveName_IsRejectedIgnoringCase()
    {
        var service = CreateService();
        await service.CreateAsync("ІПЗ-21", "2026-2027");

        var duplicate = await service.CreateAsync("іпз-21", "2027-2028");

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ErrorKeys.GroupAlreadyExists, duplicate.ErrorKey);
        Assert.Single(await AllGroupsAsync());
    }

    [Theory]
    [InlineData("", "2026-2027", ErrorKeys.GroupNameRequired)]
    [InlineData("   ", "2026-2027", ErrorKeys.GroupNameRequired)]
    [InlineData("ІПЗ-21", "", ErrorKeys.GroupYearRequired)]
    [InlineData("ІПЗ-21", "   ", ErrorKeys.GroupYearRequired)]
    public async Task Create_EmptyFields_AreRejected(string name, string year, string expectedKey)
    {
        var result = await CreateService().CreateAsync(name, year);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedKey, result.ErrorKey);
        Assert.Empty(await AllGroupsAsync());
    }

    [Fact]
    public async Task Create_FieldLimits_100ForNameAnd20ForYear()
    {
        var service = CreateService();

        var okName = await service.CreateAsync(new string('а', 100), "2026");
        var longName = await service.CreateAsync(new string('б', 101), "2026");
        var okYear = await service.CreateAsync("Г-1", new string('1', 20));
        var longYear = await service.CreateAsync("Г-2", new string('1', 21));

        Assert.True(okName.IsSuccess);
        Assert.Equal(ErrorKeys.GroupNameTooLong, longName.ErrorKey);
        Assert.True(okYear.IsSuccess);
        Assert.Equal(ErrorKeys.GroupYearTooLong, longYear.ErrorKey);
    }

    [Fact]
    public async Task Update_ChangesNameAndYear()
    {
        var service = CreateService();
        var created = await service.CreateAsync("ІПЗ-21", "2026-2027");

        var result = await service.UpdateAsync(created.Value!.StudentGroupId, "ІПЗ-22", "2027-2028");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await AllGroupsAsync());
        Assert.Equal("ІПЗ-22", saved.Name);
        Assert.Equal("2027-2028", saved.AcademicYear);
    }

    [Fact]
    public async Task Update_ToNameOfAnotherActiveGroup_IsRejected()
    {
        var service = CreateService();
        await service.CreateAsync("ІПЗ-21", "2026-2027");
        var second = await service.CreateAsync("ІПЗ-22", "2026-2027");

        var result = await service.UpdateAsync(second.Value!.StudentGroupId, "іпз-21", "2026-2027");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.GroupAlreadyExists, result.ErrorKey);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsEntityNotFound()
    {
        var result = await CreateService().UpdateAsync(999, "ІПЗ-21", "2026-2027");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task Archive_KeepsRow_AndFreesNameForNewActiveGroup()
    {
        var service = CreateService();
        var created = await service.CreateAsync("ІПЗ-21", "2026-2027");

        var archived = await service.ArchiveAsync(created.Value!.StudentGroupId);

        Assert.True(archived.IsSuccess);
        Assert.Empty(await service.GetAllAsync(includeArchived: false));
        Assert.True(Assert.Single(await service.GetAllAsync(includeArchived: true)).IsArchived);

        var again = await service.CreateAsync("ІПЗ-21", "2027-2028");
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Unarchive_WhenActiveGroupWithSameNameExists_IsRejected()
    {
        var service = CreateService();
        var first = await service.CreateAsync("ІПЗ-21", "2026-2027");
        await service.ArchiveAsync(first.Value!.StudentGroupId);
        await service.CreateAsync("ІПЗ-21", "2027-2028");

        var result = await service.UnarchiveAsync(first.Value.StudentGroupId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.GroupUnarchiveConflict, result.ErrorKey);
    }

    [Fact]
    public async Task Unarchive_WithoutConflict_Succeeds()
    {
        var service = CreateService();
        var created = await service.CreateAsync("ІПЗ-21", "2026-2027");
        await service.ArchiveAsync(created.Value!.StudentGroupId);

        var result = await service.UnarchiveAsync(created.Value.StudentGroupId);

        Assert.True(result.IsSuccess);
        Assert.Single(await service.GetAllAsync(includeArchived: false));
    }

    [Fact]
    public async Task Archive_NotFound_ReturnsEntityNotFound()
    {
        var result = await CreateService().ArchiveAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task GetAll_OrdersByYearDescendingThenName()
    {
        var service = CreateService();
        await service.CreateAsync("Б-1", "2025-2026");
        await service.CreateAsync("А-2", "2026-2027");
        await service.CreateAsync("А-1", "2026-2027");

        var all = await service.GetAllAsync();

        Assert.Equal(["А-1", "А-2", "Б-1"], all.Select(g => g.Name));
    }
}