using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class StudentService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), IStudentService
{
    public async Task<List<Student>> GetByGroupAsync(int groupId, bool includeArchived = false)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var query = context.Students
            .AsNoTracking()
            .Where(s => s.StudentGroupId == groupId);

        if (!includeArchived)
            query = query.Where(s => !s.IsArchived);

        var students = await query.ToListAsync();
        return [.. students.OrderByName(s => s.FullName)];
    }

    public async Task<Result<Student>> AddAsync(int groupId, string fullName)
    {
        var validation = ValidateStudentName(fullName);
        if (!validation.IsSuccess)
            return Result<Student>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var trimmed = validation.Value!;
        return await ExecuteInTransactionAsync(async context =>
        {
            var group = await context.StudentGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.StudentGroupId == groupId);

            if (group is null)
                return Result<Student>.Failure(ErrorKeys.EntityNotFound);

            if (group.IsArchived)
                return Result<Student>.Failure(ErrorKeys.StudentGroupArchived);

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

    public async Task<Result<Student>> UpdateAsync(int studentId, string newFullName)
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

    public Task<Result> ArchiveAsync(int studentId) => SetStudentArchiveStateAsync(studentId, true);
    public Task<Result> UnarchiveAsync(int studentId) => SetStudentArchiveStateAsync(studentId, false);

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

    private static Result<string> ValidateStudentName(string fullName)
    {
        var trimmed = fullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<string>.Failure(ErrorKeys.StudentNameRequired);
        if (trimmed.Length > ValidationConstants.StudentFullNameMaxLength)
            return Result<string>.Failure(ErrorKeys.StudentNameTooLong, ValidationConstants.StudentFullNameMaxLength);

        return Result<string>.Success(trimmed);
    }
}



