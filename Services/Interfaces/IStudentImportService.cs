using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

/// <summary>One data row read from an import file. RowNumber is the line (CSV) or row (XLSX) in the file.</summary>
public record ImportFileRow(int RowNumber, string FullName, string? GroupName);

/// <summary>Reads rows from CSV or XLSX. Kept separate so parsing can be tested without a database.</summary>
public interface IStudentFileReader
{
    Result<List<ImportFileRow>> Read(Stream stream, string fileName);
}

public enum ImportRowStatus
{
    /// <summary>Will be imported.</summary>
    Valid,
    /// <summary>Already in the file or in the database; skipped and reported.</summary>
    Duplicate,
    /// <summary>Has an error; skipped and reported.</summary>
    Invalid,
}

/// <param name="GroupId">Existing active group, or null when the group is new or unknown.</param>
/// <param name="IsNewGroup">The group does not exist yet and is created only after confirmation.</param>
public record ImportPreviewRow(
    int RowNumber,
    string FullName,
    string GroupName,
    int? GroupId,
    bool IsNewGroup,
    ImportRowStatus Status,
    string? ErrorKey = null,
    object[]? ErrorArgs = null);

public record ImportPreview(string FileName, IReadOnlyList<ImportPreviewRow> Rows)
{
    public int ValidCount => Rows.Count(r => r.Status == ImportRowStatus.Valid);
    public int DuplicateCount => Rows.Count(r => r.Status == ImportRowStatus.Duplicate);
    public int InvalidCount => Rows.Count(r => r.Status == ImportRowStatus.Invalid);

    /// <summary>Names of groups that do not exist yet, as written in the file (first spelling wins).</summary>
    public IReadOnlyList<string> NewGroupNames => Rows
        .Where(r => r.Status == ImportRowStatus.Valid && r.IsNewGroup)
        .GroupBy(r => NameNormalizer.Normalize(r.GroupName))
        .Select(g => g.First().GroupName)
        .ToList();
}

public record ImportSummary(
    int CreatedStudents,
    int CreatedGroups,
    int SkippedDuplicates,
    int InvalidRows,
    int SkippedForDeclinedGroups,
    IReadOnlyList<int> CreatedStudentIds);

/// <summary>Import of students from CSV or XLSX (FR-11).</summary>
public interface IStudentImportService
{
    /// <summary>
    /// Reads and validates the file without changing the database.
    /// <paramref name="defaultGroupId"/> is used for rows without a group.
    /// </summary>
    Task<Result<ImportPreview>> PreviewAsync(Stream stream, string fileName, int? defaultGroupId);

    /// <summary>
    /// Inserts the valid rows in one transaction and writes exactly one BulkImport audit entry.
    /// Rows for new groups are imported only if <paramref name="createNewGroups"/> is true.
    /// Rows are validated again, because the data may have changed since the preview.
    /// </summary>
    Task<Result<ImportSummary>> ImportAsync(
        ImportPreview preview,
        bool createNewGroups,
        string academicYearForNewGroups);
}
