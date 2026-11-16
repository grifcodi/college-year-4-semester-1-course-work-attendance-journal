using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceJournal.Services;

public class SubjectService(IDbContextFactory<AppDbContext> dbContextFactory) : ISubjectService
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory = dbContextFactory;

    public async Task<List<Subject>> GetAllAsync(bool includeArchived)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var query = context.Subjects.AsNoTracking();

        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Result<Subject>> CreateAsync(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<Subject>.Failure("");

        if (trimmed.Length > 150)
            return Result<Subject>.Failure("");

        var normalized = trimmed.ToUpperInvariant();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var exists = await context.Subjects.AnyAsync(s => s.NormalizedName == normalized);

        if (exists)
            return Result<Subject>.Failure("");

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var subject = new Subject
            {
                Name = trimmed,
                NormalizedName = normalized,
                IsArchived = false
            };

            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var audit = AuditHelper.CreateEntry(
                AuditAction.Create,
                AuditEntityType.Subject,
                subject.SubjectId,
                new { subject.Name, subject.IsArchived });

            context.AuditEntries.Add(audit);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<Subject>.Success(subject);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result<Subject>> UpdateAsync(int subjectId, string newName)
    {
        var trimmed = newName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<Subject>.Failure("");

        if (trimmed.Length > 150)
            return Result<Subject>.Failure("");

        var normalized = trimmed.ToUpperInvariant();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var subject = await context.Subjects.FindAsync(subjectId);

        if (subject is null)
            return Result<Subject>.Failure("");

        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            subject.Name = trimmed;
            subject.NormalizedName = normalized;

            var audit = AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.Subject,
                subject.SubjectId,
                new { subject.Name, subject.IsArchived });

            context.AuditEntries.Add(audit);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<Subject>.Success(subject);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> ArchiveAsync(int subjectId) => await SetArchiveStateAsync(subjectId, true);
    public async Task<Result> UnarchiveAsync(int subjectId) => await SetArchiveStateAsync(subjectId, false);

    private async Task<Result> SetArchiveStateAsync(int subjectId, bool isArchived)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var subject = await context.Subjects.FindAsync(subjectId);

        if (subject is null)
            return Result<Subject>.Failure("");

        if (subject.IsArchived == isArchived)
            return Result.Success();

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            subject.IsArchived = isArchived;

            var action = isArchived ? AuditAction.Archive : AuditAction.Unarchive;
            var audit = AuditHelper.CreateEntry(
                action,
                AuditEntityType.Subject,
                subject.SubjectId,
                new { subject.Name, subject.IsArchived });

            context.AuditEntries.Add(audit);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
