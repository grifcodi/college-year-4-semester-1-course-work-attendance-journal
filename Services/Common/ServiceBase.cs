using AttendanceJournal.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services.Common;

public abstract class ServiceBase(IDbContextFactory<AppDbContext> dbContextFactory)
{
    protected IDbContextFactory<AppDbContext> DbContextFactory { get; } = dbContextFactory;

    protected async Task<Result> ExecuteInTransactionAsync(
        Func<AppDbContext, Task<Result>> operation,
        string? uniqueConstraintErrorKey = null,
        params object[] errorArgs)
    {
        var result = await ExecuteInTransactionAsync(async context =>
        {
            var innerResult = await operation(context);
            return innerResult.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(innerResult.ErrorKey!, innerResult.ErrorArgs);
        }, uniqueConstraintErrorKey, errorArgs);

        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.ErrorKey!, result.ErrorArgs);
    }

    protected async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<AppDbContext, Task<Result<T>>> operation,
        string? uniqueConstraintErrorKey = null,
        params object[] errorArgs)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        // Disposing a transaction that was not committed rolls it back, so no explicit
        // RollbackAsync is needed: a failing rollback could otherwise hide the original error.
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation(context);
            if (result.IsSuccess)
                await transaction.CommitAsync();

            return result;
        }
        catch (DbUpdateException ex) when (DatabaseErrorClassifier.IsUniqueConstraintViolation(ex) && uniqueConstraintErrorKey is not null)
        {
            return Result<T>.Failure(uniqueConstraintErrorKey, errorArgs);
        }
        catch (Exception ex) when (DatabaseErrorClassifier.ClassifyErrorKey(ex) is string errorKey)
        {
            return Result<T>.Failure(errorKey);
        }
    }
}
