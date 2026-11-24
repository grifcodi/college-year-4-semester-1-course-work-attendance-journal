using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace AttendanceJournal.Services;

/// <summary>
/// Reads students from CSV (UTF-8, delimiter ',' or ';') or XLSX (first worksheet).
/// The first row is the header. Column FullName is required, Group is optional;
/// header names are case-insensitive, and the Ukrainian names ПІБ and Група are accepted too.
/// </summary>
public sealed class StudentFileReader : IStudentFileReader
{
    private static readonly string[] FullNameHeaders = ["fullname", "піб", "студент", "student"];
    private static readonly string[] GroupHeaders = ["group", "група", "groupname"];

    public Result<List<ImportFileRow>> Read(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            return extension switch
            {
                ".csv" => ReadCsv(stream),
                ".xlsx" => ReadXlsx(stream),
                _ => Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportUnsupportedFormat)
            };
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Corrupted file, wrong format behind the extension, broken quoting and so on.
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileUnreadable);
        }
    }

    private static Result<List<ImportFileRow>> ReadCsv(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileNotUtf8);
        }

        if (string.IsNullOrWhiteSpace(text))
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileEmpty);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DetectDelimiter(text),
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            DetectColumnCountChanges = false,
        };

        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read())
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileEmpty);

        csv.ReadHeader();
        var header = csv.HeaderRecord ?? [];
        var nameIndex = FindColumn(header, FullNameHeaders);
        var groupIndex = FindColumn(header, GroupHeaders);

        if (nameIndex < 0)
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportMissingFullNameColumn);

        var rows = new List<ImportFileRow>();
        while (csv.Read())
        {
            var record = csv.Parser.Record ?? [];
            if (record.All(string.IsNullOrWhiteSpace))
                continue;

            rows.Add(new ImportFileRow(
                csv.Parser.RawRow,
                Field(record, nameIndex) ?? string.Empty,
                Field(record, groupIndex)));
        }

        return Result<List<ImportFileRow>>.Success(rows);
    }

    private static Result<List<ImportFileRow>> ReadXlsx(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        if (buffer.Length == 0)
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileEmpty);

        buffer.Position = 0;
        using var workbook = new XLWorkbook(buffer);

        var sheet = workbook.Worksheets.FirstOrDefault();
        var used = sheet?.RangeUsed();
        if (sheet is null || used is null)
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportFileEmpty);

        var headerRowNumber = used.FirstRow().RowNumber();
        var lastRowNumber = used.LastRow().RowNumber();
        var firstColumn = used.FirstColumn().ColumnNumber();
        var lastColumn = used.LastColumn().ColumnNumber();

        var header = Enumerable.Range(firstColumn, lastColumn - firstColumn + 1)
            .Select(c => sheet.Cell(headerRowNumber, c).GetString())
            .ToArray();

        var nameIndex = FindColumn(header, FullNameHeaders);
        var groupIndex = FindColumn(header, GroupHeaders);

        if (nameIndex < 0)
            return Result<List<ImportFileRow>>.Failure(ErrorKeys.ImportMissingFullNameColumn);

        var rows = new List<ImportFileRow>();
        for (var r = headerRowNumber + 1; r <= lastRowNumber; r++)
        {
            var name = sheet.Cell(r, firstColumn + nameIndex).GetString().Trim();
            var group = groupIndex >= 0 ? sheet.Cell(r, firstColumn + groupIndex).GetString().Trim() : null;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(group))
                continue;

            rows.Add(new ImportFileRow(r, name, string.IsNullOrWhiteSpace(group) ? null : group));
        }

        return Result<List<ImportFileRow>>.Success(rows);
    }

    internal static string DetectDelimiter(string text)
    {
        var firstLine = text
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? string.Empty;

        var semicolons = firstLine.Count(c => c == ';');
        var commas = firstLine.Count(c => c == ',');
        return semicolons > commas ? ";" : ",";
    }

    private static int FindColumn(string[] header, string[] acceptedNames)
    {
        for (var i = 0; i < header.Length; i++)
        {
            var normalized = new string((header[i] ?? string.Empty)
                .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-')
                .ToArray())
                .ToLowerInvariant();

            if (acceptedNames.Contains(normalized))
                return i;
        }

        return -1;
    }

    private static string? Field(string[] record, int index)
    {
        if (index < 0 || index >= record.Length)
            return null;

        var value = record[index]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
