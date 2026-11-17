using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttendanceJournal.Services;

public class StudentGroupService(IDbContextFactory<AppDbContext> dbContextFactory) : ServiceBase(dbContextFactory), IStudentGroupService
{
    public async Task<List<StudentGroup>> GetAllAsync(bool includeArchived = false)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var query = context.StudentGroups.AsNoTracking();

        if (!includeArchived)
            query = query.Where(g => !g.IsArchived);

        return await query
            .OrderByDescending(g => g.AcademicYear)
            .ThenBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<Result<StudentGroup>> CreateAsync(string name, string academicYear)
    {
        var validation = ValidateGroupFields(name, academicYear);
        if (!validation.IsSuccess)
            return Result<StudentGroup>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var (trimmedName, normalizedName, trimmedYear) = validation.Value;

        return await ExecuteInTransactionAsync(async context =>
        {
            var activeExists = await context.StudentGroups
                .AnyAsync(g => !g.IsArchived && g.NormalizedName == normalizedName);
            if (activeExists)
                return Result<StudentGroup>.Failure(ErrorKeys.GroupAlreadyExists, trimmedName);

            var group = new StudentGroup
            {
                Name = trimmedName,
                NormalizedName = normalizedName,
                AcademicYear = trimmedYear,
                IsArchived = false
            };

            context.StudentGroups.Add(group);
            await context.SaveChangesAsync();

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Create,
                AuditEntityType.StudentGroup,
                group.StudentGroupId,
                new { group.Name, group.AcademicYear, group.IsArchived }));

            await context.SaveChangesAsync();
            return Result<StudentGroup>.Success(group);
        }, ErrorKeys.GroupAlreadyExists, trimmedName);
    }

    public async Task<Result<StudentGroup>> UpdateAsync(int groupId, string newName, string newAcademicYear)
    {
        var validation = ValidateGroupFields(newName, newAcademicYear);
        if (!validation.IsSuccess)
            return Result<StudentGroup>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var (trimmedName, normalizedName, trimmedYear) = validation.Value;

        return await ExecuteInTransactionAsync(async context =>
        {
            var group = await context.StudentGroups.FindAsync(groupId);
            if (group is null)
                return Result<StudentGroup>.Failure(ErrorKeys.EntityNotFound);

            if (group.Name == trimmedName && group.AcademicYear == trimmedYear)
                return Result<StudentGroup>.Success(group);

            if (!group.IsArchived)
            {
                var conflict = await context.StudentGroups
                    .AnyAsync(g => g.StudentGroupId != groupId && !g.IsArchived && g.NormalizedName == normalizedName);
                if (conflict)
                    return Result<StudentGroup>.Failure(ErrorKeys.GroupAlreadyExists, trimmedName);
            }

            group.Name = trimmedName;
            group.NormalizedName = normalizedName;
            group.AcademicYear = trimmedYear;

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.StudentGroup,
                group.StudentGroupId,
                new { group.Name, group.AcademicYear, group.IsArchived }));

            await context.SaveChangesAsync();
            return Result<StudentGroup>.Success(group);
        }, ErrorKeys.GroupAlreadyExists, trimmedName);
    }

    public Task<Result> ArchiveAsync(int groupId) => SetGroupArchiveStateAsync(groupId, true);
    public Task<Result> UnarchiveAsync(int groupId) => SetGroupArchiveStateAsync(groupId, false);
    private Task<Result> SetGroupArchiveStateAsync(int groupId, bool isArchived)
    {
        return ExecuteInTransactionAsync(async context =>
        {
            var group = await context.StudentGroups.FindAsync(groupId);
            if (group is null)
                return Result.Failure(ErrorKeys.EntityNotFound);

            if (group.IsArchived == isArchived)
                return Result.Success();

            if (!isArchived)
            {
                var conflixtExists = await context.StudentGroups
                    .AnyAsync(g => g.StudentGroupId != groupId && !g.IsArchived && g.NormalizedName == group.NormalizedName);
                if (conflixtExists)
                    return Result.Failure(ErrorKeys.GroupUnarchiveConflict, group.Name);
            }

            group.IsArchived = isArchived;

            var action = isArchived ? AuditAction.Archive : AuditAction.Unarchive;
            context.AuditEntries.Add(AuditHelper.CreateEntry(
                action,
                AuditEntityType.StudentGroup,
                group.StudentGroupId,
                new { group.Name, group.IsArchived }));

            await context.SaveChangesAsync();
            return Result.Success();
        }, ErrorKeys.GroupUnarchiveConflict);
    }

    private static Result<(string Name, string NormalizedName, string AcademicYear)> ValidateGroupFields(string name, string academicYear)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedYear = academicYear?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupNameRequired);
        if (trimmedName.Length > ValidationConstants.GroupNameMaxLength)
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupNameTooLong, ValidationConstants.GroupNameMaxLength);
        if (string.IsNullOrWhiteSpace(trimmedYear))
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupYearRequired);
        if (trimmedYear.Length > ValidationConstants.AcademicYearMaxLength)
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupYearTooLong, ValidationConstants.AcademicYearMaxLength);

        return Result<(string, string, string)>.Success((trimmedName, trimmedName.ToUpperInvariant(), trimmedYear));
    }
}
