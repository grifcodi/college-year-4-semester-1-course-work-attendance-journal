using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace AttendanceJournal.Services;

public class AttendanceExportService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISummaryService summaryService) : ServiceBase(dbContextFactory), IAttendanceExportService
{
    public const string CsvDelimiter = ";";
    public const string DateFormat = "dd.MM.yyyy";

    private sealed record LatestMark(int LessonId, int StudentId, AttendanceStatus Status, int? Grade, string? Note);

    public async Task<List<ExportRow>> GetRowsAsync(int groupId, int? subjectId, DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate > toDate)
            return [];

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var group = await context.StudentGroups.AsNoTracking().FirstOrDefaultAsync(g => g.StudentGroupId == groupId);
        if (group is null)
            return [];

        var lessonsQuery = context.Lessons
            .AsNoTracking()
            .Where(l => l.StudentGroupId == groupId && l.Date >= fromDate && l.Date <= toDate);

        if (subjectId is int sid)
            lessonsQuery = lessonsQuery.Where(l => l.SubjectId == sid);

        var lessons = await lessonsQuery
            .OrderBy(l => l.Date)
            .ThenBy(l => l.PairNumber)
            .Select(l => new { l.LessonId, l.Date, l.PairNumber, SubjectName = l.Subject.Name })
            .ToListAsync();

        if (lessons.Count == 0)
            return [];

        var lessonIds = lessons.Select(l => l.LessonId).ToList();

        var marks = await context.AttendanceVersions
            .AsNoTracking()
            .Where(v => lessonIds.Contains(v.AttendanceRecord.LessonId))
            .Where(v => v.VersionNumber == context.AttendanceVersions
                .Where(inner => inner.AttendanceRecordId == v.AttendanceRecordId)
                .Max(inner => inner.VersionNumber))
            .Select(v => new LatestMark(
                v.AttendanceRecord.LessonId,
                v.AttendanceRecord.StudentId,
                v.Status,
                v.Grade,
                v.Note))
            .ToListAsync();

        var marksByKey = marks.ToDictionary(m => (m.LessonId, m.StudentId));

        var students = (await context.Students
                .AsNoTracking()
                .Where(s => s.StudentGroupId == groupId)
                .ToListAsync())
            .OrderByName(s => s.FullName)
            .ToList();

        var rows = new List<ExportRow>();
        foreach (var lesson in lessons)
        {
            foreach (var student in students)
            {
                var hasMark = marksByKey.TryGetValue((lesson.LessonId, student.StudentId), out var mark);

                // Same rule as the lesson sheet: archived students appear only where they have a mark.
                if (student.IsArchived && !hasMark)
                    continue;

                rows.Add(new ExportRow(
                    group.Name,
                    student.FullName,
                    lesson.Date,
                    lesson.PairNumber,
                    lesson.SubjectName,
                    mark?.Status,
                    mark?.Grade,
                    mark?.Note));
            }
        }

        return rows;
    }

    public async Task<Result<int>> ExportAsync(
        int groupId,
        int? subjectId,
        DateOnly fromDate,
        DateOnly toDate,
        ExportFormat format,
        ExportLabels labels,
        Stream output)
    {
        if (fromDate > toDate)
            return Result<int>.Failure(ErrorKeys.ExportInvalidRange);

        var rows = await GetRowsAsync(groupId, subjectId, fromDate, toDate);

        // Build the whole file in memory first, so a failure never leaves a half-written file.
        using var buffer = new MemoryStream();
        if (format == ExportFormat.Csv)
        {
            WriteCsv(buffer, rows, labels);
        }
        else
        {
            var summary = await summaryService.GetGroupSummaryAsync(groupId, subjectId, fromDate, toDate);
            WriteXlsx(buffer, rows, summary, labels);
        }

        try
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (IOException)
        {
            return Result<int>.Failure(ErrorKeys.ExportWriteFailed);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<int>.Failure(ErrorKeys.ExportWriteFailed);
        }

        return Result<int>.Success(rows.Count);
    }

    internal static string StatusText(AttendanceStatus? status, ExportLabels labels) =>
        status is AttendanceStatus s && labels.StatusNames.TryGetValue(s, out var name)
            ? name
            : labels.NotMarked;

    /// <summary>UTF-8 with BOM and ';' so that Excel with a Ukrainian locale opens it correctly.</summary>
    private static void WriteCsv(Stream stream, List<ExportRow> rows, ExportLabels labels)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = CsvDelimiter };
        using var csv = new CsvWriter(writer, config, leaveOpen: true);

        foreach (var header in labels.AttendanceHeaders)
            csv.WriteField(header);
        csv.NextRecord();

        foreach (var row in rows)
        {
            csv.WriteField(row.Group);
            csv.WriteField(row.Student);
            csv.WriteField(row.Date.ToString(DateFormat, CultureInfo.InvariantCulture));
            csv.WriteField(row.Pair.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(row.Subject);
            csv.WriteField(StatusText(row.Status, labels));
            csv.WriteField(row.Grade?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(row.Comment ?? string.Empty);
            csv.NextRecord();
        }

        csv.Flush();
        writer.Flush();
    }

    private static void WriteXlsx(Stream stream, List<ExportRow> rows, GroupSummaryDto summary, ExportLabels labels)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: one row per student and lesson.
        var sheet = workbook.Worksheets.Add(SafeSheetName(labels.AttendanceSheetName, "Attendance"));
        WriteHeader(sheet, labels.AttendanceHeaders);

        var r = 2;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).Value = row.Group;
            sheet.Cell(r, 2).Value = row.Student;
            sheet.Cell(r, 3).Value = row.Date.ToDateTime(TimeOnly.MinValue); // a real date, not text
            sheet.Cell(r, 3).Style.DateFormat.Format = DateFormat;
            sheet.Cell(r, 4).Value = (double)row.Pair;
            sheet.Cell(r, 5).Value = row.Subject;
            sheet.Cell(r, 6).Value = StatusText(row.Status, labels);
            if (row.Grade is int grade)
                sheet.Cell(r, 7).Value = (double)grade;
            if (!string.IsNullOrEmpty(row.Comment))
                sheet.Cell(r, 8).Value = row.Comment;
            r++;
        }

        SetWidths(sheet, [14, 32, 12, 6, 30, 18, 8, 40]);
        sheet.SheetView.FreezeRows(1);

        // Sheet 2: attendance rate and average grade per student (spec 5.7).
        var summarySheet = workbook.Worksheets.Add(SafeSheetName(labels.SummarySheetName, "Summary"));
        WriteHeader(summarySheet, labels.SummaryHeaders);

        r = 2;
        foreach (var s in summary.Students)
        {
            summarySheet.Cell(r, 1).Value = s.FullName;
            summarySheet.Cell(r, 2).Value = (double)s.PresentCount;
            summarySheet.Cell(r, 3).Value = (double)s.LateCount;
            summarySheet.Cell(r, 4).Value = (double)s.AbsentCount;
            summarySheet.Cell(r, 5).Value = (double)s.ExcusedCount;
            summarySheet.Cell(r, 6).Value = (double)s.UnmarkedCount;
            WriteOptionalNumber(summarySheet.Cell(r, 7), s.AttendanceRatePercent);
            summarySheet.Cell(r, 8).Value = (double)s.GradedLessonsCount;
            WriteOptionalNumber(summarySheet.Cell(r, 9), s.AverageGrade);
            r++;
        }

        summarySheet.Cell(r, 1).Value = labels.GroupTotal;
        summarySheet.Cell(r, 1).Style.Font.Bold = true;
        WriteOptionalNumber(summarySheet.Cell(r, 7), summary.GroupAttendanceRatePercent);
        WriteOptionalNumber(summarySheet.Cell(r, 9), summary.GroupAverageGrade);

        SetWidths(summarySheet, [32, 10, 10, 10, 10, 12, 14, 10, 14]);
        summarySheet.SheetView.FreezeRows(1);

        workbook.SaveAs(stream);
    }

    private static void WriteHeader(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var c = 0; c < headers.Count; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        sheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteOptionalNumber(IXLCell cell, double? value)
    {
        if (value is double v)
            cell.Value = v;
        else
            cell.Value = "—";
    }

    private static void SetWidths(IXLWorksheet sheet, int[] widths)
    {
        for (var c = 0; c < widths.Length; c++)
            sheet.Column(c + 1).Width = widths[c];
    }

    /// <summary>Excel sheet names: at most 31 characters, none of : \ / ? * [ ].</summary>
    private static string SafeSheetName(string name, string fallback)
    {
        var cleaned = new string(name.Where(c => ":\\/?*[]".IndexOf(c) < 0).ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = fallback;
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
