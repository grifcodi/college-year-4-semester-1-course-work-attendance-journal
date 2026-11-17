using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceJournal.Services;

public class SubjectService(IDbContextFactory<AppDbContext> dbContextFactory) : ServiceBase(dbContextFactory), ISubjectService
{
    public async Task<List<Subject>> GetAllAsync(bool includeArchived = false)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var query = context.Subjects.AsNoTracking();

        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Result<Subject>> CreateAsync(string name)
    {
        var validatrion = ValidateAndNormalizeName(name);

        if (!validatrion.IsSuccess)
            return Result<Subject>.Failure(validatrion.ErrorKey!, validatrion.ErrorArgs);

        var (trimmed, normalized) = validatrion.Value;

        return await ExecuteInTransactionAsync(async context =>
        {
            var exists = await context.Subjects.AnyAsync(s => s.NormalizedName == normalized);
            if (exists)
                return Result<Subject>.Failure(ErrorKeys.SubjectAlreadyExists, trimmed);

            var subject = new Subject
            {
                Name = trimmed,
                NormalizedName = normalized,
                IsArchived = false
            };

            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                    AuditAction.Create,
                    AuditEntityType.Subject,
                    subject.SubjectId,
                    new { subject.Name, subject.IsArchived }));

            await context.SaveChangesAsync();
            return Result<Subject>.Success(subject);
        }, ErrorKeys.SubjectAlreadyExists, trimmed);
    }

    public async Task<Result<Subject>> UpdateAsync(int subjectId, string newName)
    {
        var validation = ValidateAndNormalizeName(newName);
        if (!validation.IsSuccess)
            return Result<Subject>.Failure(ErrorKeys.EntityNotFound);

        var (trimmed, normalized) = validation.Value;

        return await ExecuteInTransactionAsync(async context =>
        {
            var subject = await context.Subjects.FindAsync(subjectId);
            if (subject is null)
                return Result<Subject>.Failure(ErrorKeys.EntityNotFound);

            if (subject.Name == trimmed)
                return Result<Subject>.Success(subject);

            var duplicateExists = await context.Subjects
                    .AnyAsync(s => s.SubjectId != subjectId && s.NormalizedName == normalized);

            if (duplicateExists)
                return Result<Subject>.Failure(ErrorKeys.SubjectAlreadyExists, trimmed);

            subject.Name = trimmed;
            subject.NormalizedName = normalized;

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.Subject,
                subject.SubjectId,
                new { subject.Name, subject.IsArchived }));

            await context.SaveChangesAsync();
            return Result<Subject>.Success(subject);
        }, ErrorKeys.SubjectAlreadyExists, trimmed);
    }

    public async Task<Result> ArchiveAsync(int subjectId) => await SetArchiveStateAsync(subjectId, true);
    public async Task<Result> UnarchiveAsync(int subjectId) => await SetArchiveStateAsync(subjectId, false);

    private Task<Result> SetArchiveStateAsync(int subjectId, bool isArchived)
    {
        return ExecuteInTransactionAsync(async context =>
        {
            var subject = await context.Subjects.FindAsync(subjectId);

            if (subject is null)
                return Result.Failure(ErrorKeys.EntityNotFound);

            if (subject.IsArchived == isArchived)
                return Result.Success();

            subject.IsArchived = isArchived;

            var action = isArchived ? AuditAction.Archive : AuditAction.Unarchive;

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                action,
                AuditEntityType.Subject,
                subject.SubjectId,
                new { subject.Name, subject.IsArchived }));

            await context.SaveChangesAsync();
            return Result.Success();
        });
    }

    private static Result<(string Trimmed, string Normalized)> ValidateAndNormalizeName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<(string, string)>.Failure(ErrorKeys.SubjectNameRequired);

        if (trimmed.Length > 150)
            return Result<(string, string)>.Failure(ErrorKeys.SubjectNameTooLong, 150);

        return Result<(string, string)>.Success((trimmed, trimmed.ToUpperInvariant()));
    }
}
