using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using System.Globalization;

namespace AttendanceJournal.ViewModels;

/// <summary>Column header of the month grid: one lesson.</summary>
public sealed class GridColumnViewModel(MonthGridLessonDto lesson)
{
    public int LessonId { get; } = lesson.LessonId;
    public string DayText { get; } = lesson.Date.ToString("dd.MM", CultureInfo.InvariantCulture);
    public string PairText { get; } = "#" + lesson.PairNumber;
    public string Tooltip { get; } = string.Join(" · ", new[]
    {
        DisplayText.Date(lesson.Date),
        "#" + lesson.PairNumber,
        lesson.SubjectName,
        lesson.Topic,
    }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>One cell: the current mark of a student at a lesson, shown as a letter (NFR-04).</summary>
public sealed class GridCellViewModel
{
    public GridCellViewModel(int lessonId, string studentName, string lessonTooltip, MonthGridCellDto? cell)
    {
        LessonId = lessonId;
        Status = cell?.Status;
        Letter = DisplayText.StatusLetter(cell?.Status);
        GradeText = cell?.Grade?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Tooltip = $"{studentName} — {lessonTooltip}: {DisplayText.StatusName(cell?.Status)}"
                  + (cell?.Grade is int g ? $", {g}" : string.Empty);
    }

    public int LessonId { get; }
    public AttendanceStatus? Status { get; }
    public string Letter { get; }
    public string GradeText { get; }
    public bool HasGrade => GradeText.Length > 0;
    public string Tooltip { get; }

    // Used only for a light background tint; the letter is what carries the meaning.
    public bool IsPresent => Status == AttendanceStatus.Present;
    public bool IsAbsent => Status == AttendanceStatus.Absent;
    public bool IsLate => Status == AttendanceStatus.Late;
    public bool IsExcused => Status == AttendanceStatus.Excused;
}

/// <summary>One student row of the month grid.</summary>
public sealed class GridRowViewModel
{
    public GridRowViewModel(MonthGridRowDto row, IReadOnlyList<GridColumnViewModel> columns)
    {
        StudentId = row.StudentId;
        DisplayName = DisplayText.WithArchiveMark(row.FullName, row.IsArchived);
        Cells = columns
            .Select((column, i) => new GridCellViewModel(column.LessonId, row.FullName, column.Tooltip, row.Cells[i]))
            .ToList();
    }

    public int StudentId { get; }
    public string DisplayName { get; }
    public IReadOnlyList<GridCellViewModel> Cells { get; }
}
