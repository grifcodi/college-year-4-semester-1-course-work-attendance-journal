using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace AttendanceJournal.ViewModels;

/// <summary>
/// One student on the lesson sheet. Holds the saved mark and the values being edited;
/// <see cref="IsDirty"/> tells whether the row has to be saved.
/// </summary>
public sealed partial class MarkRowViewModel : ObservableObject
{
    public MarkRowViewModel(LessonStudentRowDto dto)
    {
        StudentId = dto.StudentId;
        FullName = dto.FullName;
        Saved = dto.CurrentMark;
        Reset();
    }

    public int StudentId { get; }
    public string FullName { get; }
    public AttendanceCellDto? Saved { get; }
    public bool HasSavedMark => Saved is not null;
    public string SavedVersionText => Saved is null ? string.Empty : "v" + Saved.VersionNumber;

    /// <summary>Raised whenever the edited values change, so the page can update its counters.</summary>
    public event Action? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPresent), nameof(IsAbsent), nameof(IsLate), nameof(IsExcused), nameof(StatusName))]
    public partial AttendanceStatus? Status { get; set; }

    [ObservableProperty]
    public partial string GradeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NoteText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRowError))]
    public partial string? RowError { get; set; }

    public bool HasRowError => !string.IsNullOrEmpty(RowError);

    public string StatusName => DisplayText.StatusName(Status);

    // Radio buttons bind to these; unchecking is ignored because another button gets checked instead.
    public bool IsPresent { get => Status == AttendanceStatus.Present; set { if (value) Status = AttendanceStatus.Present; } }
    public bool IsAbsent { get => Status == AttendanceStatus.Absent; set { if (value) Status = AttendanceStatus.Absent; } }
    public bool IsLate { get => Status == AttendanceStatus.Late; set { if (value) Status = AttendanceStatus.Late; } }
    public bool IsExcused { get => Status == AttendanceStatus.Excused; set { if (value) Status = AttendanceStatus.Excused; } }

    public bool IsDirty
    {
        get
        {
            var note = NormalizeNote(NoteText);
            var gradeIsValid = TryParseGrade(GradeText, out var grade);

            if (Saved is null)
                return Status is not null || !string.IsNullOrWhiteSpace(GradeText) || note is not null;

            return Status != Saved.Status || !gradeIsValid || grade != Saved.Grade || note != Saved.Note;
        }
    }

    partial void OnStatusChanged(AttendanceStatus? value) => OnEdited();
    partial void OnGradeTextChanged(string value) => OnEdited();
    partial void OnNoteTextChanged(string value) => OnEdited();

    private void OnEdited()
    {
        RowError = null;
        Changed?.Invoke();
    }

    public void Reset()
    {
        Status = Saved?.Status;
        GradeText = Saved?.Grade?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        NoteText = Saved?.Note ?? string.Empty;
        RowError = null;
    }

    /// <summary>Checks the row and builds the input for the service; sets <see cref="RowError"/> if invalid.</summary>
    public bool TryBuildInput(out MarkInput input)
    {
        input = null!;

        if (!TryParseGrade(GradeText, out var grade))
        {
            RowError = Localizer.TranslateError(Result.Failure(ErrorKeys.AttendanceInvalidGrade));
            return false;
        }

        if (Status is not AttendanceStatus status)
        {
            RowError = Localizer.Translate(UiTextKeys.RowStatusRequired);
            return false;
        }

        var note = NormalizeNote(NoteText);
        if (note?.Length > ValidationConstants.NoteMaxLength)
        {
            RowError = Localizer.TranslateError(
                Result.Failure(ErrorKeys.AttendanceNoteTooLong, ValidationConstants.NoteMaxLength));
            return false;
        }

        input = new MarkInput(StudentId, status, grade, note);
        return true;
    }

    /// <summary>Empty text means "no grade". Otherwise only a whole number 0–100 is accepted (FR-06).</summary>
    public static bool TryParseGrade(string? text, out int? grade)
    {
        grade = null;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return true;

        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value is < 0 or > 100)
            return false;

        grade = value;
        return true;
    }

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
