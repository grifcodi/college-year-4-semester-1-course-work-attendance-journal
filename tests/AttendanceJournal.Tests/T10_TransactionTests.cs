using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T10_TransactionTests : SqliteInMemoryTestBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class TestService(IDbContextFactory<AppDbContext> factory) : ServiceBase(factory)
    {
        public Task<Result> Run(
            Func<AppDbContext, Task<Result>> operation,
            string? uniqueKey = null) =>
            ExecuteInTransactionAsync(operation, uniqueKey);

        public Task<Result<T>> RunTyped<T>(
            Func<AppDbContext, Task<Result<T>>> operation,
            string? uniqueKey = null) =>
            ExecuteInTransactionAsync(operation, uniqueKey);
    }

    private TestService CreateService() => new(ContextFactory);

    private async Task AssertDatabaseEmptyAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        Assert.Empty(await context.Subjects.ToListAsync(Ct));
        Assert.Empty(await context.AuditEntries.ToListAsync(Ct));
    }

    private static Subject NewSubject(string name) =>
        new() { Name = name, NormalizedName = name.ToUpperInvariant() };

    [Fact]
    public async Task ExceptionAfterSave_RollsBackEverything_AndRethrows()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Run(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            await context.SaveChangesAsync(Ct);
            context.AuditEntries.Add(new AuditEntry
            {
                TimeStamp = DateTime.UtcNow,
                Action = AuditAction.Create,
                EntityType = AuditEntityType.Subject,
                EntityId = 1,
                PayloadJson = "{}"
            });
            await context.SaveChangesAsync(Ct);

            throw new InvalidOperationException("Імітація збою");
        }));

        await AssertDatabaseEmptyAsync();
    }

    [Fact]
    public async Task FailureResult_RollsBackChanges_AndReturnsTheSameResult()
    {
        var service = CreateService();

        var result = await service.Run(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            await context.SaveChangesAsync(Ct);
            return Result.Failure("Err_Test", 42);
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Err_Test", result.ErrorKey);
        Assert.Equal([42], result.ErrorArgs);
        await AssertDatabaseEmptyAsync();
    }

    [Fact]
    public async Task SuccessResult_CommitsChanges()
    {
        var service = CreateService();

        var result = await service.Run(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            await context.SaveChangesAsync(Ct);
            return Result.Success();
        });

        Assert.True(result.IsSuccess);
        await using var check = await ContextFactory.CreateDbContextAsync(Ct);
        Assert.Single(await check.Subjects.ToListAsync(Ct));
    }

    [Fact]
    public async Task TypedOverload_CommitsOnSuccess_AndRollsBackOnFailure()
    {
        var service = CreateService();

        var ok = await service.RunTyped(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            await context.SaveChangesAsync(Ct);
            return Result<int>.Success(7);
        });
        var failed = await service.RunTyped(async context =>
        {
            context.Subjects.Add(NewSubject("Фізика"));
            await context.SaveChangesAsync(Ct);
            return Result<int>.Failure("Err_Test");
        });

        Assert.Equal(7, ok.Value);
        Assert.False(failed.IsSuccess);
        await using var check = await ContextFactory.CreateDbContextAsync(Ct);
        var names = await check.Subjects.Select(s => s.Name).ToListAsync(Ct);
        Assert.Equal(["Математика"], names);
    }

    [Fact]
    public async Task UniqueViolation_WithKey_ReturnsFailure_AndRollsBack()
    {
        var service = CreateService();

        var result = await service.Run(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            context.Subjects.Add(NewSubject("математика"));
            await context.SaveChangesAsync(Ct);
            return Result.Success();
        }, uniqueKey: "Err_Test_Unique");

        Assert.False(result.IsSuccess);
        Assert.Equal("Err_Test_Unique", result.ErrorKey);
        await AssertDatabaseEmptyAsync();
    }

    [Fact]
    public async Task UniqueViolation_WithoutKey_IsRethrown()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<DbUpdateException>(() => service.Run(async context =>
        {
            context.Subjects.Add(NewSubject("Математика"));
            context.Subjects.Add(NewSubject("математика"));
            await context.SaveChangesAsync(Ct);
            return Result.Success();
        }));

        await AssertDatabaseEmptyAsync();
    }

    [Fact]
    public async Task ForeignKeyViolation_IsNotReportedAsUniqueViolation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<DbUpdateException>(() => service.Run(async context =>
        {
            context.Students.Add(new Student { StudentGroupId = 999, FullName = "Шевченко Тарас" });
            await context.SaveChangesAsync(Ct);
            return Result.Success();
        }, uniqueKey: "Err_Test_Unique"));
    }
}

public class ResultTests
{
    [Fact]
    public void Success_HasNoErrorKey()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorKey);
        Assert.Empty(result.ErrorArgs);
    }

    [Fact]
    public void Failure_KeepsKeyAndArguments()
    {
        var result = Result.Failure("Err_Test", 150, "x");

        Assert.False(result.IsSuccess);
        Assert.Equal("Err_Test", result.ErrorKey);
        Assert.Equal([150, "x"], result.ErrorArgs);
    }

    [Fact]
    public void TypedFailure_HasDefaultValue()
    {
        var result = Result<string>.Failure("Err_Test");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void TypedSuccess_CarriesValue()
    {
        var result = Result<int>.Success(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }
}