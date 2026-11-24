using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class StudentImportService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IStudentFileReader fileReader) : ServiceBase(dbContextFactory), IStudentImportService
{
    public async Task<Result<ImportPreview>> PreviewAsync(Stream stream, string fileName, int? defaultGroupId)
    {
        var read = fileReader.Read(stream, fileName);
        if (!read.IsSuccess)
            return Result<ImportPreview>.Failure(read.ErrorKey!, read.ErrorArgs);

        var fileRows = read.Value!;
        if (fileRows.Count == 0)
            return Result<ImportPreview>.Failure(ErrorKeys.ImportFileEmpty);

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var activeGroups = await context.StudentGroups
            .AsNoTracking()
            .Where(g => !g.IsArchived)
            .ToListAsync();

        StudentGroup? defaultGroup = null;
        if (defaultGroupId is int id)
            defaultGroup = await context.StudentGroups.AsNoTracking().FirstOrDefaultAsync(g => g.StudentGroupId == id);

        var groupsByName = activeGroups.ToDictionary(g => g.NormalizedName);
        var namesByGroup = await LoadNamesByGroupAsync(context, activeGroups.Select(g => g.StudentGroupId).ToList());

        // Key: group key + upper-case name. Value: row number of the first occurrence.
        var seenInFile = new Dictionary<string, int>();
        var previewRows = new List<ImportPreviewRow>(fileRows.Count);

        foreach (var row in fileRows)
        {
            var fullName = row.FullName.Trim();
            var groupText = row.GroupName?.Trim();

            // 1. Which group?
            int? groupId = null;
            var isNewGroup = false;
            string groupDisplay;

            if (string.IsNullOrEmpty(groupText))
            {
                if (defaultGroup is null)
                {
                    previewRows.Add(Invalid(row, fullName, string.Empty, ErrorKeys.ImportNoTargetGroup));
                    continue;
                }

                groupDisplay = defaultGroup.Name;
                if (defaultGroup.IsArchived)
                {
                    previewRows.Add(Invalid(row, fullName, groupDisplay, ErrorKeys.StudentGroupArchived));
                    continue;
                }

                groupId = defaultGroup.StudentGroupId;
            }
            else
            {
                groupDisplay = groupText;
                if (groupText.Length > ValidationConstants.GroupNameMaxLength)
                {
                    previewRows.Add(Invalid(row, fullName, groupDisplay, ErrorKeys.ImportRowGroupNameTooLong,
                        ValidationConstants.GroupNameMaxLength));
                    continue;
                }

                if (groupsByName.TryGetValue(NameNormalizer.Normalize(groupText), out var existing))
                {
                    groupId = existing.StudentGroupId;
                    groupDisplay = existing.Name;
                }
                else
                {
                    isNewGroup = true;
                }
            }

            // 2. Is the name valid?
            if (string.IsNullOrEmpty(fullName))
            {
                previewRows.Add(Invalid(row, fullName, groupDisplay, ErrorKeys.ImportRowNameRequired));
                continue;
            }

            if (fullName.Length > ValidationConstants.StudentFullNameMaxLength)
            {
                previewRows.Add(Invalid(row, fullName, groupDisplay, ErrorKeys.ImportRowNameTooLong,
                    ValidationConstants.StudentFullNameMaxLength));
                continue;
            }

            // 3. Duplicate in the file?
            var groupKey = groupId is int gid ? $"id:{gid}" : $"new:{NameNormalizer.Normalize(groupDisplay)}";
            var fileKey = groupKey + "|" + fullName.ToUpperInvariant();
            if (seenInFile.TryGetValue(fileKey, out var firstRow))
            {
                previewRows.Add(new ImportPreviewRow(row.RowNumber, fullName, groupDisplay, groupId, isNewGroup,
                    ImportRowStatus.Duplicate, ErrorKeys.ImportRowDuplicateInFile, [firstRow]));
                continue;
            }

            seenInFile[fileKey] = row.RowNumber;

            // 4. Duplicate in the database? Archived students count too (D6).
            if (groupId is int existingGroupId
                && namesByGroup.TryGetValue(existingGroupId, out var names)
                && names.Contains(fullName))
            {
                previewRows.Add(new ImportPreviewRow(row.RowNumber, fullName, groupDisplay, groupId, isNewGroup,
                    ImportRowStatus.Duplicate, ErrorKeys.ImportRowDuplicateInDatabase, [fullName]));
                continue;
            }

            previewRows.Add(new ImportPreviewRow(row.RowNumber, fullName, groupDisplay, groupId, isNewGroup,
                ImportRowStatus.Valid));
        }

        return Result<ImportPreview>.Success(new ImportPreview(Path.GetFileName(fileName), previewRows));
    }

    public async Task<Result<ImportSummary>> ImportAsync(
        ImportPreview preview,
        bool createNewGroups,
        string academicYearForNewGroups)
    {
        var candidates = preview.Rows.Where(r => r.Status == ImportRowStatus.Valid).ToList();
        var skippedForDeclinedGroups = createNewGroups ? 0 : candidates.Count(r => r.IsNewGroup);
        if (!createNewGroups)
            candidates = candidates.Where(r => !r.IsNewGroup).ToList();

        if (candidates.Count == 0)
            return Result<ImportSummary>.Failure(ErrorKeys.ImportNothingToImport);

        var year = academicYearForNewGroups?.Trim() ?? string.Empty;
        if (createNewGroups && preview.NewGroupNames.Count > 0)
        {
            if (year.Length == 0)
                return Result<ImportSummary>.Failure(ErrorKeys.GroupYearRequired);
            if (year.Length > ValidationConstants.AcademicYearMaxLength)
                return Result<ImportSummary>.Failure(ErrorKeys.GroupYearTooLong, ValidationConstants.AcademicYearMaxLength);
        }

        return await ExecuteInTransactionAsync(async context =>
        {
            var activeGroups = await context.StudentGroups
                .Where(g => !g.IsArchived)
                .ToListAsync();

            var groupsById = activeGroups.ToDictionary(g => g.StudentGroupId);
            var groupsByName = activeGroups.ToDictionary(g => g.NormalizedName);
            var namesByGroup = await LoadNamesByGroupAsync(context, groupsById.Keys.ToList());

            var createdGroups = new List<StudentGroup>();
            var createdStudents = new List<Student>();
            var skippedDuplicates = preview.DuplicateCount;

            foreach (var row in candidates)
            {
                StudentGroup? group;
                if (row.GroupId is int gid)
                {
                    // The group may have been archived or deleted after the preview.
                    if (!groupsById.TryGetValue(gid, out group))
                        return Result<ImportSummary>.Failure(ErrorKeys.StudentGroupArchived);
                }
                else
                {
                    var normalized = NameNormalizer.Normalize(row.GroupName);
                    if (!groupsByName.TryGetValue(normalized, out group))
                    {
                        group = new StudentGroup
                        {
                            Name = row.GroupName.Trim(),
                            NormalizedName = normalized,
                            AcademicYear = year,
                            IsArchived = false
                        };
                        context.StudentGroups.Add(group);
                        await context.SaveChangesAsync();

                        groupsByName[normalized] = group;
                        groupsById[group.StudentGroupId] = group;
                        createdGroups.Add(group);
                    }
                }

                if (!namesByGroup.TryGetValue(group.StudentGroupId, out var names))
                {
                    names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    namesByGroup[group.StudentGroupId] = names;
                }

                // Someone may have added the same student after the preview.
                if (!names.Add(row.FullName))
                {
                    skippedDuplicates++;
                    continue;
                }

                var student = new Student
                {
                    StudentGroupId = group.StudentGroupId,
                    FullName = row.FullName,
                    IsArchived = false
                };
                context.Students.Add(student);
                createdStudents.Add(student);
            }

            if (createdStudents.Count == 0)
                return Result<ImportSummary>.Failure(ErrorKeys.ImportNothingToImport);

            await context.SaveChangesAsync();

            var summary = new ImportSummary(
                createdStudents.Count,
                createdGroups.Count,
                skippedDuplicates,
                preview.InvalidCount,
                skippedForDeclinedGroups,
                createdStudents.Select(s => s.StudentId).ToList());

            // A bulk import is one audit entry (FR-12), written in the same transaction.
            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.BulkImport,
                AuditEntityType.Student,
                null,
                new
                {
                    preview.FileName,
                    CreatedGroups = createdGroups.Count,
                    CreatedGroupIds = createdGroups.Select(g => g.StudentGroupId).ToList(),
                    CreatedStudents = summary.CreatedStudents,
                    SkippedDuplicates = summary.SkippedDuplicates,
                    InvalidRows = summary.InvalidRows,
                    SkippedForDeclinedGroups = summary.SkippedForDeclinedGroups,
                    CreatedStudentIds = summary.CreatedStudentIds
                }));

            await context.SaveChangesAsync();
            return Result<ImportSummary>.Success(summary);
        });
    }

    private static async Task<Dictionary<int, HashSet<string>>> LoadNamesByGroupAsync(
        AppDbContext context,
        List<int> groupIds)
    {
        var students = await context.Students
            .AsNoTracking()
            .Where(s => groupIds.Contains(s.StudentGroupId))
            .Select(s => new { s.StudentGroupId, s.FullName })
            .ToListAsync();

        return students
            .GroupBy(s => s.StudentGroupId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private static ImportPreviewRow Invalid(
        ImportFileRow row,
        string fullName,
        string groupName,
        string errorKey,
        params object[] args) =>
        new(row.RowNumber, fullName, groupName, null, false, ImportRowStatus.Invalid, errorKey, args);
}
