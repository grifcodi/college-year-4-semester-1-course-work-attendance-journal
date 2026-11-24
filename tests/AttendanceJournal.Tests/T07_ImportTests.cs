using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AttendanceJournal.Tests;

/// <summary>T-07: import of students (FR-11) and its single audit entry (FR-12).</summary>
public class T07_ImportTests : AttendanceTestBase
{
    private readonly StudentFileReader _reader = new();
    private StudentImportService Import => new(ContextFactory, _reader);

    private static MemoryStream Utf8(string text, bool bom = false)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        if (bom)
            bytes = [0xEF, 0xBB, 0xBF, .. bytes];
        return new MemoryStream(bytes);
    }

    private static MemoryStream Xlsx(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Students");
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                sheet.Cell(r + 1, c + 1).Value = rows[r][c];

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task<int> CountStudentsAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        return await context.Students.CountAsync(Ct);
    }

    // ---------- Reading files ----------

    [Fact]
    public void Csv_WithSemicolonAndBom_IsRead_WithRowNumbers()
    {
        var result = _reader.Read(Utf8("FullName;Group\nШевченко Тарас;ІПЗ-21\n\nЄрмоленко Ґанна;ІПЗ-22\n", bom: true), "s.csv");

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value!,
            r => { Assert.Equal("Шевченко Тарас", r.FullName); Assert.Equal("ІПЗ-21", r.GroupName); Assert.Equal(2, r.RowNumber); },
            r => { Assert.Equal("Єрмоленко Ґанна", r.FullName); Assert.Equal("ІПЗ-22", r.GroupName); });
    }

    [Fact]
    public void Csv_WithComma_CaseInsensitiveHeader_AndQuotedName_IsRead()
    {
        var result = _reader.Read(Utf8("fullname,GROUP\n\"Коваль, Олена\",КН-1\n"), "s.CSV");

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value!);
        Assert.Equal("Коваль, Олена", row.FullName);
        Assert.Equal("КН-1", row.GroupName);
    }

    [Fact]
    public void Csv_WithoutGroupColumn_LeavesGroupEmpty()
    {
        var result = _reader.Read(Utf8("ПІБ\nШевченко Тарас\n"), "s.csv");

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(result.Value!).GroupName);
    }

    [Fact]
    public void Csv_NotUtf8_IsRejected()
    {
        // "Шевченко" in Windows-1251: not valid UTF-8.
        byte[] cp1251 = [0x46, 0x75, 0x6C, 0x6C, 0x4E, 0x61, 0x6D, 0x65, 0x0A, 0xD8, 0xE5, 0xE2, 0xF7, 0xE5, 0xED, 0xEA, 0xEE];

        var result = _reader.Read(new MemoryStream(cp1251), "s.csv");

        Assert.Equal(ErrorKeys.ImportFileNotUtf8, result.ErrorKey);
    }

    [Theory]
    [InlineData("Name;Group\nA;B\n", ErrorKeys.ImportMissingFullNameColumn)]
    [InlineData("", ErrorKeys.ImportFileEmpty)]
    [InlineData("   \n  ", ErrorKeys.ImportFileEmpty)]
    public void Csv_BadContent_GivesClearError(string content, string expectedKey)
    {
        Assert.Equal(expectedKey, _reader.Read(Utf8(content), "s.csv").ErrorKey);
    }

    [Fact]
    public void UnsupportedExtension_IsRejected()
    {
        Assert.Equal(ErrorKeys.ImportUnsupportedFormat, _reader.Read(Utf8("FullName\nA\n"), "s.txt").ErrorKey);
    }

    [Fact]
    public void Xlsx_IsRead_FromFirstSheet()
    {
        var result = _reader.Read(Xlsx(["FullName", "Group"], ["Шевченко Тарас", "ІПЗ-21"], ["", ""], ["Ющенко Ія", ""]), "s.xlsx");

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value!,
            r => { Assert.Equal(2, r.RowNumber); Assert.Equal("ІПЗ-21", r.GroupName); },
            r => { Assert.Equal(4, r.RowNumber); Assert.Equal("Ющенко Ія", r.FullName); Assert.Null(r.GroupName); });
    }

    [Fact]
    public void CorruptedXlsx_IsReportedAsUnreadable()
    {
        Assert.Equal(ErrorKeys.ImportFileUnreadable, _reader.Read(Utf8("not a zip file"), "s.xlsx").ErrorKey);
    }

    // ---------- Preview ----------

    [Fact]
    public async Task Preview_ClassifiesRows_WithRowNumbersAndReasons()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var archivedId = await AddStudentAsync(groupId, "Архівний Студент");
        await Students.ArchiveAsync(archivedId);
        var longName = new string('Я', ValidationConstants.StudentFullNameMaxLength + 1);

        var csv = "FullName;Group\n"
                  + "Шевченко Тарас;ІПЗ-21\n"     // 2 valid
                  + ";ІПЗ-21\n"                    // 3 empty name
                  + longName + ";ІПЗ-21\n"         // 4 too long
                  + "шевченко тарас;іпз-21\n"      // 5 duplicate of row 2 (case-insensitive)
                  + "Архівний Студент;ІПЗ-21\n"    // 6 already in DB (archived counts, D6)
                  + "Нова Людина;КН-99\n";         // 7 new group

        var result = await Import.PreviewAsync(Utf8(csv), "students.csv", defaultGroupId: null);

        Assert.True(result.IsSuccess);
        var rows = result.Value!.Rows;
        Assert.Equal([2, 3, 4, 5, 6, 7], rows.Select(r => r.RowNumber));
        Assert.Equal(ImportRowStatus.Valid, rows[0].Status);
        Assert.Equal(ErrorKeys.ImportRowNameRequired, rows[1].ErrorKey);
        Assert.Equal(ErrorKeys.ImportRowNameTooLong, rows[2].ErrorKey);
        Assert.Equal(ImportRowStatus.Duplicate, rows[3].Status);
        Assert.Equal(ErrorKeys.ImportRowDuplicateInFile, rows[3].ErrorKey);
        Assert.Equal(2, rows[3].ErrorArgs![0]);
        Assert.Equal(ErrorKeys.ImportRowDuplicateInDatabase, rows[4].ErrorKey);
        Assert.True(rows[5].IsNewGroup);
        Assert.Equal(ImportRowStatus.Valid, rows[5].Status);
        Assert.Equal(["КН-99"], result.Value.NewGroupNames);
        Assert.Equal((2, 2, 2), (result.Value.ValidCount, result.Value.DuplicateCount, result.Value.InvalidCount));
    }

    [Fact]
    public async Task Preview_RowsWithoutGroup_UseDefaultGroup_OrAreInvalidWithoutIt()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");

        var withDefault = await Import.PreviewAsync(Utf8("FullName\nШевченко Тарас\n"), "s.csv", groupId);
        var withoutDefault = await Import.PreviewAsync(Utf8("FullName\nШевченко Тарас\n"), "s.csv", null);

        Assert.Equal(groupId, Assert.Single(withDefault.Value!.Rows).GroupId);
        Assert.Equal(ErrorKeys.ImportNoTargetGroup, Assert.Single(withoutDefault.Value!.Rows).ErrorKey);
    }

    [Fact]
    public async Task Preview_DoesNotChangeTheDatabase()
    {
        await CreateGroupAsync("ІПЗ-21");
        var auditBefore = (await ContextFactory.GetAuditAsync()).Count;

        await Import.PreviewAsync(Utf8("FullName;Group\nА Б;ІПЗ-21\nВ Г;НОВА\n"), "s.csv", null);

        Assert.Equal(0, await CountStudentsAsync());
        Assert.Equal(auditBefore, (await ContextFactory.GetAuditAsync()).Count);
    }

    // ---------- Import ----------

    [Fact]
    public async Task Import_AddsValidRows_SkipsOthers_AndWritesExactlyOneBulkImportEntry()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        await AddStudentAsync(groupId, "Наявний Студент");
        var preview = (await Import.PreviewAsync(
            Utf8("FullName;Group\nШевченко Тарас;ІПЗ-21\nНаявний Студент;ІПЗ-21\n;ІПЗ-21\nКоваль Олена;КН-1\nБондар Іван;кн-1\n"),
            "students.csv", null)).Value!;

        var result = await Import.ImportAsync(preview, createNewGroups: true, "2026-2027");

        Assert.True(result.IsSuccess);
        var summary = result.Value!;
        Assert.Equal(3, summary.CreatedStudents);
        Assert.Equal(1, summary.CreatedGroups);
        Assert.Equal(1, summary.SkippedDuplicates);
        Assert.Equal(1, summary.InvalidRows);
        Assert.Equal(4, await CountStudentsAsync());

        var groups = await Groups.GetAllAsync();
        var newGroup = Assert.Single(groups, g => g.Name == "КН-1");
        Assert.Equal("2026-2027", newGroup.AcademicYear);
        Assert.Equal(2, (await Students.GetByGroupAsync(newGroup.StudentGroupId)).Count);

        var bulk = Assert.Single(await ContextFactory.GetAuditAsync(), a => a.Action == AuditAction.BulkImport);
        Assert.Equal(AuditEntityType.Student, bulk.EntityType);
        Assert.Contains("students.csv", bulk.PayloadJson);
        Assert.Contains("\"CreatedStudents\":3", bulk.PayloadJson);
    }

    [Fact]
    public async Task Import_DecliningNewGroups_SkipsTheirRows()
    {
        await CreateGroupAsync("ІПЗ-21");
        var preview = (await Import.PreviewAsync(
            Utf8("FullName;Group\nШевченко Тарас;ІПЗ-21\nКоваль Олена;КН-1\n"), "s.csv", null)).Value!;

        var result = await Import.ImportAsync(preview, createNewGroups: false, "2026-2027");

        Assert.Equal(1, result.Value!.CreatedStudents);
        Assert.Equal(1, result.Value.SkippedForDeclinedGroups);
        Assert.Single(await Groups.GetAllAsync());
    }

    [Fact]
    public async Task Import_WithNothingLeft_Fails_AndWritesNoAuditEntry()
    {
        var preview = (await Import.PreviewAsync(Utf8("FullName;Group\nКоваль Олена;КН-1\n"), "s.csv", null)).Value!;

        var result = await Import.ImportAsync(preview, createNewGroups: false, "2026-2027");

        Assert.Equal(ErrorKeys.ImportNothingToImport, result.ErrorKey);
        Assert.Equal(0, await CountStudentsAsync());
        Assert.DoesNotContain(await ContextFactory.GetAuditAsync(), a => a.Action == AuditAction.BulkImport);
    }

    [Fact]
    public async Task Import_WhenGroupWasArchivedAfterPreview_RollsBackEverything()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var preview = (await Import.PreviewAsync(
            Utf8("FullName;Group\nКоваль Олена;КН-1\nШевченко Тарас;ІПЗ-21\n"), "s.csv", null)).Value!;
        await Groups.ArchiveAsync(groupId);

        var result = await Import.ImportAsync(preview, createNewGroups: true, "2026-2027");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await CountStudentsAsync());
        Assert.DoesNotContain(await Groups.GetAllAsync(includeArchived: true), g => g.Name == "КН-1");
        Assert.DoesNotContain(await ContextFactory.GetAuditAsync(), a => a.Action == AuditAction.BulkImport);
    }

    [Fact]
    public async Task Import_StudentAddedAfterPreview_IsSkippedAsDuplicate()
    {
        var groupId = await CreateGroupAsync("ІПЗ-21");
        var preview = (await Import.PreviewAsync(
            Utf8("FullName\nШевченко Тарас\nКоваль Олена\n"), "s.csv", groupId)).Value!;
        await AddStudentAsync(groupId, "Шевченко Тарас");

        var result = await Import.ImportAsync(preview, createNewGroups: true, "2026-2027");

        Assert.Equal(1, result.Value!.CreatedStudents);
        Assert.Equal(1, result.Value.SkippedDuplicates);
    }
}
