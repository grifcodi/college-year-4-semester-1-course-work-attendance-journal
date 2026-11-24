using AttendanceJournal.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AttendanceJournal.ViewModels;

/// <summary>A lesson in the list of lessons on the selected date.</summary>
public sealed partial class LessonItemViewModel(Lesson lesson) : ObservableObject
{
    public int LessonId { get; } = lesson.LessonId;
    public int StudentGroupId { get; } = lesson.StudentGroupId;
    public int PairNumber { get; } = lesson.PairNumber;
    public DateOnly Date { get; } = lesson.Date;
    public string SubjectName { get; } = lesson.Subject?.Name ?? string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string? Topic { get; set; } = lesson.Topic;

    public string Title => $"{PairNumber}. {SubjectName}";
}
