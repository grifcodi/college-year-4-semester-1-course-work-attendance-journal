using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using ClosedXML.Excel;
using System.Text;

namespace AttendanceJournal.Tests;

/// <summary>T-08: export of attendance (FR-10): row count, unmarked rows, Ukrainian text, file formats.</summary>
public class T08_ExportTests : AttendanceTestBase
{
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 30);

    private AttendanceExportService Export => new(ContextFactory, new SummaryService(ContextFactory));

    /// <summary>3 active students, 1 archived with a mark, 1 archived without marks; 2 lessons.</summary>
    private async Task<(int GroupId, int SubjectId)> SeedAsync()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var subjectId = await CreateSubjectAsync("Інструментальні засоби");
        var otherSubject = await CreateSubjectAsync("Фізика");

        var a = await AddStudentAsync(groupId, "Ґудзь Єлизавета");
        var b = await AddStudentAsync(groupId, "Іщенко Їжак");
        await AddStudentAsync(groupId, "Шевченко Тарас");
        var archivedWithMark = await AddStudentAsync(groupId, "Архівна З Відміткою");
        var archivedWithoutMark = await AddStudentAsync(groupId, "Архівний Без Відміток");

        var l1 = await CreateLessonAsync(groupId, subjectId, day: 3, pair: 1);
        var l2 = await CreateLessonAsync(groupId, subjectId, day: 5, pair: 2);
        await CreateLessonAsync(groupId, otherSubject, day: 4, pair: 1);
        await Lessons.CreateAsync(groupId, subjectId, new DateOnly(2026, 12, 1), 1, null); // outside the range

        await MarkAsync(l1, a, AttendanceStatus.Present, grade: 95, note: "Відповідь; «чудово»");
        await MarkAsync(l1, b, AttendanceStatus.Absent);
        await MarkAsync(l2, archivedWithMark, AttendanceStatus.Late);
        await Students.ArchiveAsync(archivedWithMark);
        await Students.ArchiveAsync(archivedWithoutMark);

        return (groupId, subjectId);
    }

    [Fact]
    public async Task Rows_IncludeUnmarkedStudents_AndArchivedOnlyWhereMarked()
    {
        var (groupId, subjectId) = await SeedAsync();

        var rows = await Export.GetRowsAsync(groupId, subjectId, From, To);

        // Lesson 1: 3 active students. Lesson 2: 3 active + the archived student with a mark.
        Assert.Equal(7, rows.Count);
        Assert.Equal(3, rows.Count(r => r.Date == new DateOnly(2026, 11, 3)));
        Assert.Equal(4, rows.Count(r => r.Date == new DateOnly(2026, 11, 5)));
        Assert.DoesNotContain(rows, r => r.Student == "Архівний Без Відміток");
        Assert.Equal(4, rows.Count(r => r.Status is null));
        Assert.Contains(rows, r => r.Student == "Ґудзь Єлизавета" && r.Grade == 95 && r.Status == AttendanceStatus.Present);
    }

    [Fact]
    public async Task Rows_WithoutSubjectFilter_IncludeAllSubjects_OrderedByDateAndPair()
    {
        var (groupId, _) = await SeedAsync();

        var rows = await Export.GetRowsAsync(groupId, null, From, To);

        Assert.Equal(10, rows.Count);
        Assert.Equal(rows.OrderBy(r => r.Date).ThenBy(r => r.Pair).Select(r => (r.Date, r.Pair)), rows.Select(r => (r.Date, r.Pair)));
    }

    [Fact]
    public async Task Csv_HasBom_Semicolons_UkrainianText_AndOneLinePerRow()
    {
        var (groupId, subjectId) = await SeedAsync();
        using var output = new MemoryStream();

        var result = await Export.ExportAsync(groupId, subjectId, From, To, ExportFormat.Csv, ExportLabels.English, output);

        Assert.True(result.IsSuccess);
        var bytes = output.ToArray();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);

        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(result.Value + 1, lines.Length);
        Assert.Equal("Group;Student;Date;Pair;Subject;Status;Grade;Comment", lines[0]);
        Assert.Contains("ІПЗ-21;Ґудзь Єлизавета;03.11.2026;1;Інструментальні засоби;Present;95;\"Відповідь; «чудово»\"", lines);
        Assert.Contains(lines, l => l.Contains("Шевченко Тарас") && l.Contains(";Not marked;"));
    }

    [Fact]
    public async Task Xlsx_HasAttendanceAndSummarySheets_WithMatchingRowCount()
    {
        var (groupId, subjectId) = await SeedAsync();
        using var output = new MemoryStream();

        var result = await Export.ExportAsync(groupId, subjectId, From, To, ExportFormat.Xlsx, ExportLabels.English, output);

        Assert.True(result.IsSuccess);
        output.Position = 0;
        using var workbook = new XLWorkbook(output);
        Assert.Equal(["Attendance", "Summary"], workbook.Worksheets.Select(w => w.Name));

        var sheet = workbook.Worksheet("Attendance");
        Assert.Equal(result.Value + 1, sheet.LastRowUsed()!.RowNumber());
        var markedRow = Enumerable.Range(2, result.Value)
            .Single(r => sheet.Cell(r, 2).GetString() == "Ґудзь Єлизавета" && sheet.Cell(r, 3).GetDateTime().Day == 3);
        Assert.Equal(95, sheet.Cell(markedRow, 7).GetDouble());
        Assert.Equal("Present", sheet.Cell(markedRow, 6).GetString());

        var summary = workbook.Worksheet("Summary");
        Assert.Equal("Student", summary.Cell(1, 1).GetString());
        Assert.Contains(Enumerable.Range(2, 10).Select(r => summary.Cell(r, 1).GetString()), n => n == "Іщенко Їжак");
    }

    [Fact]
    public async Task InvalidRange_IsRejected_AndNothingIsWritten()
    {
        var (groupId, subjectId) = await SeedAsync();
        using var output = new MemoryStream();

        var result = await Export.ExportAsync(groupId, subjectId, To, From, ExportFormat.Csv, ExportLabels.English, output);

        Assert.Equal(ErrorKeys.ExportInvalidRange, result.ErrorKey);
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public async Task ArchivedGroup_CanStillBeExported()
    {
        var (groupId, subjectId) = await SeedAsync();
        await Groups.ArchiveAsync(groupId);

        var rows = await Export.GetRowsAsync(groupId, subjectId, From, To);

        Assert.Equal(7, rows.Count);
    }
}
