using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>
/// Lessons and marking (FR-04 – FR-07, FR-16). The teacher picks a group and a date, selects or creates
/// a lesson, sets statuses, grades and comments, and saves the whole sheet in one action.
/// Editing a saved mark requires a reason.
/// </summary>
public partial class JournalPageViewModel(
    IStudentGroupService groupService,
    ISubjectService subjectService,
    ILessonService lessonService,
    IAttendanceService attendanceService,
    IConfirmationService confirmation,
    IErrorLog errorLog) : OperationViewModelBase(errorLog), IActivatablePage
{
    public const int MaxPairNumber = 8;

    private bool _suppressSelectionHandlers;
    private int _lessonsVersion;
    private int _sheetVersion;

    public ObservableCollection<StudentGroup> Groups { get; } = [];
    public ObservableCollection<Subject> Subjects { get; } = [];
    public ObservableCollection<LessonItemViewModel> Lessons { get; } = [];
    public ObservableCollection<MarkRowViewModel> Rows { get; } = [];
    public ObservableCollection<HistoryItemViewModel> HistoryItems { get; } = [];
    public IReadOnlyList<int> PairNumbers { get; } = Enumerable.Range(1, MaxPairNumber).ToList();

    [ObservableProperty]
    public partial StudentGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial DateTime? SelectedDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLesson))]
    public partial LessonItemViewModel? SelectedLesson { get; set; }

    [ObservableProperty]
    public partial Subject? NewLessonSubject { get; set; }

    [ObservableProperty]
    public partial int NewLessonPair { get; set; } = 1;

    [ObservableProperty]
    public partial string NewLessonTopic { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TopicInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReasonInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnsavedChanges), nameof(UnsavedText))]
    public partial int UnsavedCount { get; set; }

    [ObservableProperty]
    public partial bool IsReasonRequired { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryOpen))]
    public partial MarkRowViewModel? HistoryRow { get; set; }

    [ObservableProperty]
    public partial string HistoryTitle { get; set; } = string.Empty;

    public bool HasLesson => SelectedLesson is not null;
    public bool HasNoLessons => Lessons.Count == 0;
    public bool HasUnsavedChanges => UnsavedCount > 0;
    public bool IsHistoryOpen => HistoryRow is not null;
    public string UnsavedText => Localizer.Translate(UiTextKeys.UnsavedChangesCount, UnsavedCount);

    // ---------- Loading ----------

    public Task ActivateAsync() => RunSafeAsync("Journal.Activate", async () =>
    {
        await LoadPickersAsync();

        // Do not throw away marks the teacher has not saved yet.
        if (!HasUnsavedChanges)
            await LoadLessonsCoreAsync(SelectedLesson?.LessonId);
    }, allowRetry: true);

    /// <summary>Opens a lesson chosen elsewhere, for example from the month grid (FR-08).</summary>
    public Task OpenLessonAsync(int lessonId) => RunSafeAsync("Journal.OpenLesson", async () =>
    {
        if (HasUnsavedChanges && !await ConfirmDiscardAsync())
            return;

        var lesson = await lessonService.GetByIdAsync(lessonId);
        if (lesson is null)
        {
            ErrorMessage = Localizer.TranslateError(Result.Failure(ErrorKeys.EntityNotFound));
            return;
        }

        await LoadPickersAsync();

        RunSilently(() =>
        {
            var group = Groups.FirstOrDefault(g => g.StudentGroupId == lesson.StudentGroupId);
            if (group is null && lesson.StudentGroup is not null)
            {
                // Lessons of an archived group can still be opened and corrected.
                group = lesson.StudentGroup;
                Groups.Add(group);
            }

            SelectedGroup = group;
            SelectedDate = lesson.Date.ToDateTime(TimeOnly.MinValue);
        });

        await LoadLessonsCoreAsync(lessonId);
    }, allowRetry: true);

    private async Task LoadPickersAsync()
    {
        var groups = await groupService.GetAllAsync(includeArchived: false);
        var subjects = await subjectService.GetAllAsync(includeArchived: false);

        RunSilently(() =>
        {
            var groupId = SelectedGroup?.StudentGroupId;
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);
            SelectedGroup = Groups.FirstOrDefault(g => g.StudentGroupId == groupId) ?? Groups.FirstOrDefault();

            var subjectId = NewLessonSubject?.SubjectId;
            Subjects.Clear();
            foreach (var s in subjects)
                Subjects.Add(s);
            NewLessonSubject = Subjects.FirstOrDefault(s => s.SubjectId == subjectId) ?? Subjects.FirstOrDefault();
        });
    }

    private async Task LoadLessonsCoreAsync(int? selectLessonId)
    {
        var version = ++_lessonsVersion;
        var group = SelectedGroup;
        var date = SelectedDate;

        List<Lesson> lessons = [];
        if (group is not null && date is not null)
        {
            var day = DateOnly.FromDateTime(date.Value);
            lessons = await lessonService.GetByRangeAsync(group.StudentGroupId, day, day);
            if (version != _lessonsVersion)
                return;
        }

        RunSilently(() =>
        {
            Lessons.Clear();
            foreach (var lesson in lessons)
                Lessons.Add(new LessonItemViewModel(lesson));

            SelectedLesson = Lessons.FirstOrDefault(l => l.LessonId == selectLessonId) ?? Lessons.FirstOrDefault();
        });

        OnPropertyChanged(nameof(HasNoLessons));

        // Suggest the first free pair for a new lesson.
        var taken = lessons.Select(l => l.PairNumber).ToHashSet();
        NewLessonPair = PairNumbers.FirstOrDefault(p => !taken.Contains(p), 1);

        await LoadSheetCoreAsync();
    }

    private async Task LoadSheetCoreAsync()
    {
        var version = ++_sheetVersion;
        CloseHistory();

        var lesson = SelectedLesson;
        if (lesson is null)
        {
            ReplaceRows([]);
            TopicInput = string.Empty;
            return;
        }

        var sheet = await attendanceService.GetLessonSheetAsync(lesson.LessonId);
        if (version != _sheetVersion)
            return;

        TopicInput = lesson.Topic ?? string.Empty;
        ReplaceRows(sheet.Select(dto => new MarkRowViewModel(dto)));
    }

    private void ReplaceRows(IEnumerable<MarkRowViewModel> rows)
    {
        foreach (var row in Rows)
            row.Changed -= UpdateUnsavedState;

        Rows.Clear();
        foreach (var row in rows)
        {
            row.Changed += UpdateUnsavedState;
            Rows.Add(row);
        }

        ReasonInput = string.Empty;
        UpdateUnsavedState();
    }

    private void UpdateUnsavedState()
    {
        UnsavedCount = Rows.Count(r => r.IsDirty);
        IsReasonRequired = Rows.Any(r => r.HasSavedMark && r.IsDirty);
    }

    // ---------- Reacting to the pickers ----------

    partial void OnSelectedGroupChanged(StudentGroup? oldValue, StudentGroup? newValue)
    {
        if (!_suppressSelectionHandlers)
            _ = OnContextChangedAsync(() => SelectedGroup = oldValue);
    }

    partial void OnSelectedDateChanged(DateTime? oldValue, DateTime? newValue)
    {
        if (!_suppressSelectionHandlers)
            _ = OnContextChangedAsync(() => SelectedDate = oldValue);
    }

    partial void OnSelectedLessonChanged(LessonItemViewModel? oldValue, LessonItemViewModel? newValue)
    {
        if (!_suppressSelectionHandlers)
            _ = OnLessonChangedAsync(oldValue);
    }

    private Task OnContextChangedAsync(Action revert) => RunSafeAsync("Journal.LoadLessons", async () =>
    {
        if (HasUnsavedChanges && !await ConfirmDiscardAsync())
        {
            RunSilently(revert);
            return;
        }

        ClearError();
        InfoMessage = null;
        await LoadLessonsCoreAsync(null);
    }, allowRetry: true);

    private Task OnLessonChangedAsync(LessonItemViewModel? previous) => RunSafeAsync("Journal.LoadSheet", async () =>
    {
        if (HasUnsavedChanges && !await ConfirmDiscardAsync())
        {
            RunSilently(() => SelectedLesson = previous);
            return;
        }

        ClearError();
        InfoMessage = null;
        await LoadSheetCoreAsync();
    }, allowRetry: true);

    // ---------- Lessons ----------

    [RelayCommand]
    private Task CreateLessonAsync() => RunSafeAsync("Journal.CreateLesson", async () =>
    {
        if (SelectedGroup is null || SelectedDate is null || NewLessonSubject is null)
            return;

        ClearError();
        InfoMessage = null;

        if (HasUnsavedChanges && !await ConfirmDiscardAsync())
            return;

        var result = await lessonService.CreateAsync(
            SelectedGroup.StudentGroupId,
            NewLessonSubject.SubjectId,
            DateOnly.FromDateTime(SelectedDate.Value),
            NewLessonPair,
            NewLessonTopic);

        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        NewLessonTopic = string.Empty;
        await LoadLessonsCoreAsync(result.Value!.LessonId);
        ShowInfo(Localizer.Translate(UiTextKeys.LessonCreatedInfo));
    }, allowRetry: true);

    [RelayCommand]
    private Task SaveTopicAsync() => RunSafeAsync("Journal.SaveTopic", async () =>
    {
        if (SelectedLesson is not { } lesson)
            return;

        ClearError();
        InfoMessage = null;

        var result = await lessonService.UpdateTopicAsync(lesson.LessonId, TopicInput);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        // Update in place: reloading the sheet would drop unsaved marks.
        lesson.Topic = result.Value!.Topic;
        TopicInput = lesson.Topic ?? string.Empty;
        ShowInfo(Localizer.Translate(UiTextKeys.TopicSavedInfo));
    }, allowRetry: true);

    // ---------- Marks ----------

    [RelayCommand]
    private Task SaveMarksAsync() => RunSafeAsync("Journal.SaveMarks", async () =>
    {
        if (SelectedLesson is not { } lesson)
            return;

        ClearError();
        InfoMessage = null;

        var dirtyRows = Rows.Where(r => r.IsDirty).ToList();
        if (dirtyRows.Count == 0)
            return;

        var inputs = new List<MarkInput>(dirtyRows.Count);
        var allValid = true;
        foreach (var row in dirtyRows)
        {
            if (row.TryBuildInput(out var input))
                inputs.Add(input);
            else
                allValid = false;
        }

        if (!allValid)
        {
            ErrorMessage = Localizer.Translate(UiTextKeys.FixRowErrors);
            return;
        }

        string? reason = null;
        if (IsReasonRequired)
        {
            reason = ReasonInput.Trim();
            if (reason.Length == 0)
            {
                ErrorMessage = Localizer.TranslateError(Result.Failure(ErrorKeys.AttendanceReasonRequired));
                return;
            }

            if (reason.Length < ValidationConstants.ReasonMinLength)
            {
                ErrorMessage = Localizer.TranslateError(
                    Result.Failure(ErrorKeys.AttendanceReasonTooShort, ValidationConstants.ReasonMinLength));
                return;
            }
        }

        var result = await attendanceService.SaveMarksAsync(lesson.LessonId, inputs, reason);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await LoadSheetCoreAsync();
        ShowInfo(Localizer.Translate(UiTextKeys.SavedMarksInfo, result.Value!.Count));
    }, allowRetry: true);

    [RelayCommand]
    private void DiscardChanges()
    {
        foreach (var row in Rows)
            row.Reset();

        ReasonInput = string.Empty;
        ClearError();
        UpdateUnsavedState();
    }

    [RelayCommand]
    private Task MarkAllPresentAsync() => RunSafeAsync("Journal.MarkAllPresent", async () =>
    {
        if (SelectedLesson is not { } lesson)
            return;

        ClearError();
        InfoMessage = null;

        if (HasUnsavedChanges && !await ConfirmDiscardAsync())
            return;

        var result = await attendanceService.MarkAllPresentAsync(lesson.LessonId);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await LoadSheetCoreAsync();
        ShowInfo(Localizer.Translate(UiTextKeys.MarkedAllPresentInfo, result.Value));
    }, allowRetry: true);

    /// <summary>Keyboard shortcut Ctrl+1…4 (NFR-03): 1 = present, 2 = absent, 3 = late, 4 = excused.</summary>
    public static void SetStatusByShortcut(MarkRowViewModel row, int shortcutNumber)
    {
        row.Status = shortcutNumber switch
        {
            1 => AttendanceStatus.Present,
            2 => AttendanceStatus.Absent,
            3 => AttendanceStatus.Late,
            4 => AttendanceStatus.Excused,
            _ => row.Status,
        };
    }

    // ---------- History and restore ----------

    [RelayCommand]
    private Task ShowHistoryAsync(MarkRowViewModel? row) => RunSafeAsync("Journal.History", async () =>
    {
        if (row is null || SelectedLesson is not { } lesson)
            return;

        var versions = await attendanceService.GetHistoryAsync(lesson.LessonId, row.StudentId);
        var current = versions.Count == 0 ? 0 : versions.Max(v => v.VersionNumber);

        HistoryItems.Clear();
        foreach (var version in versions)
            HistoryItems.Add(new HistoryItemViewModel(version, version.VersionNumber == current));

        HistoryTitle = Localizer.Translate(UiTextKeys.HistoryHeader, row.FullName);
        HistoryRow = row;
    }, allowRetry: true);

    [RelayCommand]
    private void CloseHistory()
    {
        HistoryRow = null;
        HistoryItems.Clear();
    }

    [RelayCommand]
    private Task RestoreVersionAsync(HistoryItemViewModel? item) => RunSafeAsync("Journal.RestoreVersion", async () =>
    {
        var historyRow = HistoryRow;
        if (item is null || historyRow?.Saved is not { } saved)
            return;

        ClearError();
        InfoMessage = null;

        var studentId = historyRow.StudentId;
        var unsavedElsewhere = Rows.Any(r => r.IsDirty);
        if (unsavedElsewhere && !await ConfirmDiscardAsync())
            return;

        var typedReason = ReasonInput.Trim();
        var reason = typedReason.Length >= ValidationConstants.ReasonMinLength
            ? typedReason
            : Localizer.Translate(UiTextKeys.RestoreReasonDefault, item.VersionNumber);

        var result = await attendanceService.RestoreVersionAsync(saved.AttendanceRecordId, item.VersionNumber, reason);
        if (!result.IsSuccess)
        {
            ErrorMessage = Localizer.TranslateError(result);
            return;
        }

        await LoadSheetCoreAsync();
        var row = Rows.FirstOrDefault(r => r.StudentId == studentId);
        if (row is not null)
            await ShowHistoryAsync(row);

        ShowInfo(Localizer.Translate(UiTextKeys.RestoredInfo, item.VersionNumber));
    }, allowRetry: true);

    // ---------- Helpers ----------

    private Task<bool> ConfirmDiscardAsync() =>
        confirmation.ConfirmAsync(Localizer.Translate(UiTextKeys.ConfirmDiscardChanges));

    private void RunSilently(Action action)
    {
        var previous = _suppressSelectionHandlers;
        _suppressSelectionHandlers = true;
        try
        {
            action();
        }
        finally
        {
            _suppressSelectionHandlers = previous;
        }
    }
}
