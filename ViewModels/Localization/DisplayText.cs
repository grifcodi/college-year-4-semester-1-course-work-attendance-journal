using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using System.Globalization;

namespace AttendanceJournal.ViewModels.Localization;

/// <summary>Localized texts for enums and values shown in lists (status, audit action, entity type).</summary>
public static class DisplayText
{
    public static string StatusName(AttendanceStatus? status) => Localizer.Translate(status switch
    {
        AttendanceStatus.Present => UiTextKeys.StatusPresent,
        AttendanceStatus.Absent => UiTextKeys.StatusAbsent,
        AttendanceStatus.Late => UiTextKeys.StatusLate,
        AttendanceStatus.Excused => UiTextKeys.StatusExcused,
        _ => UiTextKeys.StatusNotMarked,
    });

    /// <summary>Short letter for grids (NFR-04: status is never shown by color alone).</summary>
    public static string StatusLetter(AttendanceStatus? status) => Localizer.Translate(status switch
    {
        AttendanceStatus.Present => UiTextKeys.LetterPresent,
        AttendanceStatus.Absent => UiTextKeys.LetterAbsent,
        AttendanceStatus.Late => UiTextKeys.LetterLate,
        AttendanceStatus.Excused => UiTextKeys.LetterExcused,
        _ => UiTextKeys.LetterNotMarked,
    });

    public static string ActionName(AuditAction action) => Localizer.Translate(action switch
    {
        AuditAction.Create => UiTextKeys.ActionCreate,
        AuditAction.Update => UiTextKeys.ActionUpdate,
        AuditAction.Archive => UiTextKeys.ActionArchive,
        AuditAction.Unarchive => UiTextKeys.ActionUnarchive,
        AuditAction.MarkAttendance => UiTextKeys.ActionMarkAttendance,
        AuditAction.BulkImport => UiTextKeys.ActionBulkImport,
        AuditAction.RestoreVersion => UiTextKeys.ActionRestoreVersion,
        _ => action.ToString(),
    });

    public static string EntityName(AuditEntityType entityType) => Localizer.Translate(entityType switch
    {
        AuditEntityType.StudentGroup => UiTextKeys.EntityStudentGroup,
        AuditEntityType.Student => UiTextKeys.EntityStudent,
        AuditEntityType.Subject => UiTextKeys.EntitySubject,
        AuditEntityType.Lesson => UiTextKeys.EntityLesson,
        AuditEntityType.AttendanceRecord => UiTextKeys.EntityAttendanceRecord,
        AuditEntityType.Setting => UiTextKeys.EntitySetting,
        _ => entityType.ToString(),
    });

    public static string WithArchiveMark(string name, bool isArchived) =>
        isArchived ? Localizer.Translate(UiTextKeys.ArchivedSuffix, name) : name;

    public static string Percent(double? value) =>
        value is double v ? v.ToString("0.##", CultureInfo.InvariantCulture) + " %" : Localizer.Translate(UiTextKeys.NoValue);

    public static string Grade(double? value) =>
        value is double v ? v.ToString("0.0", CultureInfo.InvariantCulture) : Localizer.Translate(UiTextKeys.NoValue);

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string LocalTime(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    public static string MonthTitle(int year, int month)
    {
        try
        {
            var culture = new CultureInfo(Localizer.CurrentLanguage == AppLanguage.En ? "en-US" : "uk-UA");
            var name = culture.DateTimeFormat.GetMonthName(month);
            return $"{culture.TextInfo.ToTitleCase(name)} {year}";
        }
        catch (CultureNotFoundException)
        {
            return $"{month:00}.{year}";
        }
    }

    /// <summary>Labels for export files in the current UI language.</summary>
    public static ExportLabels CreateExportLabels() => new(
        Localizer.Translate(UiTextKeys.AttendanceSheetName),
        Localizer.Translate(UiTextKeys.SummarySheetName),
        [
            Localizer.Translate(UiTextKeys.ColumnGroup),
            Localizer.Translate(UiTextKeys.ColumnStudent),
            Localizer.Translate(UiTextKeys.ColumnDate),
            Localizer.Translate(UiTextKeys.ColumnPair),
            Localizer.Translate(UiTextKeys.ColumnSubject),
            Localizer.Translate(UiTextKeys.ColumnStatus),
            Localizer.Translate(UiTextKeys.ColumnGrade),
            Localizer.Translate(UiTextKeys.ColumnComment),
        ],
        [
            Localizer.Translate(UiTextKeys.ColumnStudent),
            Localizer.Translate(UiTextKeys.StatusPresent),
            Localizer.Translate(UiTextKeys.StatusLate),
            Localizer.Translate(UiTextKeys.StatusAbsent),
            Localizer.Translate(UiTextKeys.StatusExcused),
            Localizer.Translate(UiTextKeys.StatusNotMarked),
            Localizer.Translate(UiTextKeys.ColumnRate),
            Localizer.Translate(UiTextKeys.ColumnGradesCount),
            Localizer.Translate(UiTextKeys.ColumnAverage),
        ],
        new Dictionary<AttendanceStatus, string>
        {
            [AttendanceStatus.Present] = StatusName(AttendanceStatus.Present),
            [AttendanceStatus.Absent] = StatusName(AttendanceStatus.Absent),
            [AttendanceStatus.Late] = StatusName(AttendanceStatus.Late),
            [AttendanceStatus.Excused] = StatusName(AttendanceStatus.Excused),
        },
        StatusName(null),
        Localizer.Translate(UiTextKeys.GroupTotalRow));
}
