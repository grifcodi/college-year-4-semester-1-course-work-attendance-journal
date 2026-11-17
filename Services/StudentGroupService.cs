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

public class StudentGroupService(IDbContextFactory<AppDbContext> dbContextFactory) : ServiceBase(dbContextFactory), IStudentGroupService
{
    public async Task<List<StudentGroup>> GetGroupAsync(bool includeArchived = false)
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

    public async Task<Result<StudentGroup>> CreateGroupAsync(string name, string academicYear)
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
                AcademicYear = trimmedName,
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

    public async Task<Result<StudentGroup>> UpdateGroupAsync(int groupId, string newName, string newAcademicYear)
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

    public Task<Result> ArchiveGroupAsync(int groupId) => SetGroupArchiveStateAsync(groupId, true);
    public Task<Result> UnarchiveGroupAsync(int groupId) => SetGroupArchiveStateAsync(groupId, false);
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

    public async Task<List<Student>> GetStudentsByGroupAsync(int groupId, bool includeArchived = false)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var query = context.Students
            .AsNoTracking()
            .Where(s => s.StudentGroupId == groupId);

        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);

        return await query.OrderBy(s => s.FullName).ToListAsync();
    }

    public async Task<Result<Student>> AddStudentAsync(int groupId, string fullName)
    {
        var validation = ValidateStudentName(fullName);
        if (!validation.IsSuccess)
            return Result<Student>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var trimmed = validation.Value!;

        return await ExecuteInTransactionAsync(async context =>
        {
            var groupExists = await context.StudentGroups.AnyAsync(g => g.StudentGroupId == groupId);
            if (!groupExists)
                return Result<Student>.Failure(ErrorKeys.EntityNotFound);

            var existingNames = await context.Students
                .Where(s => s.StudentGroupId == groupId)
                .Select(s => s.FullName)
                .ToListAsync();

            if (existingNames.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase)))
                return Result<Student>.Failure(ErrorKeys.StudentAlreadyExists, trimmed);

            var student = new Student
            {
                StudentGroupId = groupId,
                FullName = trimmed,
                IsArchived = false
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Create,
                AuditEntityType.Student,
                student.StudentId,
                new { student.StudentGroupId, student.FullName, student.IsArchived }));

            await context.SaveChangesAsync();
            return Result<Student>.Success(student);
        });
    }

    public async Task<Result<Student>> UpdateStudentAsync(int studentId, string newFullName)
    {
        var validation = ValidateStudentName(newFullName);
        if (!validation.IsSuccess)
            return Result<Student>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var trimmed = validation.Value!;

        return await ExecuteInTransactionAsync(async context =>
        {
            var student = await context.Students.FindAsync(studentId);
            if (student is null)
                return Result<Student>.Failure(ErrorKeys.EntityNotFound);

            if (student.FullName == trimmed)
                return Result<Student>.Success(student);

            var siblingNames = await context.Students
                .Where(s => s.StudentGroupId == student.StudentGroupId && s.StudentId != studentId)
                .Select(s => s.FullName)
                .ToListAsync();

            if (siblingNames.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase)))
                return Result<Student>.Failure(ErrorKeys.StudentAlreadyExists, trimmed);

            student.FullName = trimmed;

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.Student,
                student.StudentId,
                new { student.StudentGroupId, student.FullName, student.IsArchived }));

            await context.SaveChangesAsync();
            return Result<Student>.Success(student);
        });
    }

    public Task<Result> ArchiveStudentAsync(int studentId) => SetStudentArchiveStateAsync(studentId, true);
    public Task<Result> UnarchiveStudentAsync(int studentId) => SetStudentArchiveStateAsync(studentId, false);

    private Task<Result> SetStudentArchiveStateAsync(int studentId, bool isArchived)
    {
        return ExecuteInTransactionAsync(async context =>
        {
            var student = await context.Students.FindAsync(studentId);
            if (student is null)
                return Result.Failure(ErrorKeys.EntityNotFound);

            if (student.IsArchived == isArchived)
                return Result.Success();

            student.IsArchived = isArchived;

            var action = isArchived ? AuditAction.Archive : AuditAction.Unarchive;
            context.AuditEntries.Add(AuditHelper.CreateEntry(
                action,
                AuditEntityType.Student,
                student.StudentId,
                new { student.StudentGroupId, student.FullName, student.IsArchived }));

            await context.SaveChangesAsync();
            return Result.Success();
        });
    }

    private static Result<(string Name, string NormalizedName, string AcademicYear)> ValidateGroupFields(string name, string academicYear)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedYear = academicYear?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupNameRequired);
        if (trimmedName.Length > 100)
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupNameTooLong, 100);
        if (string.IsNullOrWhiteSpace(trimmedYear))
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupYearReqired);
        if (trimmedYear.Length > 20)
            return Result<(string, string, string)>.Failure(ErrorKeys.GroupYearTooLong, 20);

        return Result<(string, string, string)>.Success((trimmedYear, trimmedName.ToUpperInvariant(), trimmedYear));
    }

    private static Result<string> ValidateStudentName(string fullName)
    {
        var trimmed = fullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<string>.Failure(ErrorKeys.StudentNameRequired);
        if (trimmed.Length > 150)
            return Result<string>.Failure(ErrorKeys.StudentNameTooLong, 150);

        return Result<string>.Success(trimmed);
    }
}
