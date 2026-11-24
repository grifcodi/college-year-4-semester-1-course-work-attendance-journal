using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public enum ExportFormat
{
    Csv,
    Xlsx,
}

/// <summary>One exported line: one student at one lesson. Status null = not marked.</summary>
public record ExportRow(
    string Group,
    string Student,
    DateOnly Date,
    int Pair,
    string Subject,
    AttendanceStatus? Status,
    int? Grade,
    string? Comment);

/// <summary>
/// Texts written into the file. The view model fills them from the current UI language;
/// services do not depend on the localization layer.
/// </summary>
public record ExportLabels(
    string AttendanceSheetName,
    string SummarySheetName,
    IReadOnlyList<string> AttendanceHeaders,
    IReadOnlyList<string> SummaryHeaders,
    IReadOnlyDictionary<AttendanceStatus, string> StatusNames,
    string NotMarked,
    string GroupTotal)
{
    /// <summary>English labels, used by tests and as a fallback.</summary>
    public static ExportLabels English { get; } = new(
        "Attendance",
        "Summary",
        ["Group", "Student", "Date", "Pair", "Subject", "Status", "Grade", "Comment"],
        ["Student", "Present", "Late", "Absent", "Excused", "Not marked", "Attendance %", "Grades", "Average grade"],
        new Dictionary<AttendanceStatus, string>
        {
            [AttendanceStatus.Present] = "Present",
            [AttendanceStatus.Absent] = "Absent",
            [AttendanceStatus.Late] = "Late",
            [AttendanceStatus.Excused] = "Excused",
        },
        "Not marked",
        "Group total");
}

/// <summary>Export of attendance for a group and a date range (FR-10).</summary>
public interface IAttendanceExportService
{
    /// <summary>
    /// Rows in the order date, pair, student. Every lesson has a row for each student on its sheet
    /// (active students and archived students with a mark), so unmarked students are included.
    /// </summary>
    Task<List<ExportRow>> GetRowsAsync(int groupId, int? subjectId, DateOnly fromDate, DateOnly toDate);

    /// <summary>Writes the file to <paramref name="output"/> and returns the number of data rows.</summary>
    Task<Result<int>> ExportAsync(
        int groupId,
        int? subjectId,
        DateOnly fromDate,
        DateOnly toDate,
        ExportFormat format,
        ExportLabels labels,
        Stream output);
}
