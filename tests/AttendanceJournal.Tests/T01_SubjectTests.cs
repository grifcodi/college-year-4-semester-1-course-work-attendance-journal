using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T01_SubjectTests : SqliteInMemoryTestBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SubjectService CreateService() => new(ContextFactory);

    private async Task<List<Subject>> AllSubjectsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.Subjects.AsNoTracking().OrderBy(s => s.SubjectId).ToListAsync(Ct);
    }

    [Fact]
    public async Task Create_TrimsName_AndStoresNormalizedName()
    {
        var result = await CreateService().CreateAsync("  Математика  ");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await AllSubjectsAsync());
        Assert.Equal("Математика", saved.Name);
        Assert.Equal("МАТЕМАТИКА", saved.NormalizedName);
        Assert.False(saved.IsArchived);
    }

    [Fact]
    public async Task Create_DuplicateNameIgnoringCase_IsRejected()
    {
        var service = CreateService();
        await service.CreateAsync("Математика");

        var duplicate = await service.CreateAsync("МАТЕМАТИКА");

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectAlreadyExists, duplicate.ErrorKey);
        Assert.Single(await AllSubjectsAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyName_IsRejected(string name)
    {
        var result = await CreateService().CreateAsync(name);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectNameRequired, result.ErrorKey);
        Assert.Empty(await AllSubjectsAsync());
    }

    [Fact]
    public async Task Create_Name150Chars_IsAccepted_And151IsRejected()
    {
        var service = CreateService();

        var ok = await service.CreateAsync(new string('а', 150));
        var tooLong = await service.CreateAsync(new string('б', 151));

        Assert.True(ok.IsSuccess);
        Assert.False(tooLong.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectNameTooLong, tooLong.ErrorKey);
        Assert.Single(await AllSubjectsAsync());
    }

    [Fact]
    public async Task Update_ChangesNameAndNormalizedName()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Фізика");

        var result = await service.UpdateAsync(created.Value!.SubjectId, "Астрономія");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await AllSubjectsAsync());
        Assert.Equal("Астрономія", saved.Name);
        Assert.Equal("АСТРОНОМІЯ", saved.NormalizedName);
    }

    [Fact]
    public async Task Update_ToNameOfAnotherSubject_IsRejected_AndKeepsOriginal()
    {
        var service = CreateService();
        await service.CreateAsync("Математика");
        var second = await service.CreateAsync("Фізика");

        var result = await service.UpdateAsync(second.Value!.SubjectId, "математика");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectAlreadyExists, result.ErrorKey);
        var subjects = await AllSubjectsAsync();
        Assert.Contains(subjects, s => s.Name == "Фізика");
    }

    [Fact]
    public async Task Update_EmptyName_ReturnsValidationError_NotEntityNotFound()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Фізика");

        var result = await service.UpdateAsync(created.Value!.SubjectId, "  ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectNameRequired, result.ErrorKey);
    }

    [Fact]
    public async Task Update_OnlyChangesCase_IsAllowed()
    {
        var service = CreateService();
        var created = await service.CreateAsync("математика");

        var result = await service.UpdateAsync(created.Value!.SubjectId, "Математика");

        Assert.True(result.IsSuccess);
        Assert.Equal("Математика", Assert.Single(await AllSubjectsAsync()).Name);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsEntityNotFound()
    {
        var result = await CreateService().UpdateAsync(999, "Хімія");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task Archive_HidesFromDefaultList_ButKeepsRow()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Хімія");

        var result = await service.ArchiveAsync(created.Value!.SubjectId);

        Assert.True(result.IsSuccess);
        Assert.Empty(await service.GetAllAsync(includeArchived: false));
        var all = await service.GetAllAsync(includeArchived: true);
        Assert.True(Assert.Single(all).IsArchived);
    }

    [Fact]
    public async Task Archive_KeepsNameReserved_SoSameNameCannotBeCreatedAgain()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Хімія");
        await service.ArchiveAsync(created.Value!.SubjectId);

        var again = await service.CreateAsync("хімія");

        Assert.False(again.IsSuccess);
        Assert.Equal(ErrorKeys.SubjectAlreadyExists, again.ErrorKey);
    }

    [Fact]
    public async Task Unarchive_RestoresSubjectToDefaultList()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Хімія");
        await service.ArchiveAsync(created.Value!.SubjectId);

        var result = await service.UnarchiveAsync(created.Value.SubjectId);

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
    public async Task GetAll_ReturnsSubjectsOrderedByName()
    {
        var service = CreateService();
        await service.CreateAsync("Фізика");
        await service.CreateAsync("Алгебра");
        await service.CreateAsync("Біологія");

        var all = await service.GetAllAsync();

        Assert.Equal(["Алгебра", "Біологія", "Фізика"], all.Select(s => s.Name));
    }
}