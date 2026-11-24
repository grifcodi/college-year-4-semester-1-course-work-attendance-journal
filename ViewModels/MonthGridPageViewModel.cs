using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>Month grid of a group (FR-08): students × lessons, filter by subject, click opens the lesson.</summary>
public partial class MonthGridPageViewModel(
    IStudentGroupService groupService,
    ISubjectService subjectService,
    IMonthGridService monthGridService,
    IErrorLog errorLog) : OperationViewModelBase(errorLog), IActivatablePage
{
    private bool _suppressReload;
    private int _loadVersion;

    public ObservableCollection<StudentGroup> Groups { get; } = [];
    public ObservableCollection<SelectOption<int?>> SubjectOptions { get; } = [];
    public ObservableCollection<GridColumnViewModel> Columns { get; } = [];
    public ObservableCollection<GridRowViewModel> Rows { get; } = [];

    /// <summary>Raised when the user opens a cell; the shell switches to the journal page.</summary>
    public event Action<int>? OpenLessonRequested;

    [ObservableProperty]
    public partial StudentGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial bool ShowArchivedGroups { get; set; }

    [ObservableProperty]
    public partial SelectOption<int?>? SelectedSubjectOption { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    public partial int Year { get; set; } = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    public partial int Month { get; set; } = DateTime.Today.Month;

    [ObservableProperty]
    public partial bool HasNoLessons { get; set; }

    public string MonthTitle => DisplayText.MonthTitle(Year, Month);

    partial void OnSelectedGroupChanged(StudentGroup? value) => ReloadIfAllowed();
    partial void OnSelectedSubjectOptionChanged(SelectOption<int?>? value) => ReloadIfAllowed();
    partial void OnShowArchivedGroupsChanged(bool value) => _ = ActivateAsync();

    private void ReloadIfAllowed()
    {
        if (!_suppressReload)
            _ = LoadGridAsync();
    }

    public Task ActivateAsync() => RunSafeAsync("MonthGrid.Activate", async () =>
    {
        var groups = await groupService.GetAllAsync(ShowArchivedGroups);
        var subjects = await subjectService.GetAllAsync(includeArchived: true);

        _suppressReload = true;
        try
        {
            var groupId = SelectedGroup?.StudentGroupId;
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);
            SelectedGroup = Groups.FirstOrDefault(g => g.StudentGroupId == groupId) ?? Groups.FirstOrDefault();

            var subjectId = SelectedSubjectOption?.Value;
            SubjectOptions.Clear();
            SubjectOptions.Add(new SelectOption<int?>(null, Localizer.Translate(UiTextKeys.AllSubjects)));
            foreach (var s in subjects)
                SubjectOptions.Add(new SelectOption<int?>(s.SubjectId, DisplayText.WithArchiveMark(s.Name, s.IsArchived)));
            SelectedSubjectOption = SubjectOptions.FirstOrDefault(o => o.Value == subjectId) ?? SubjectOptions[0];
        }
        finally
        {
            _suppressReload = false;
        }

        await LoadGridCoreAsync();
    }, allowRetry: true);

    [RelayCommand]
    public Task LoadGridAsync() => RunSafeAsync("MonthGrid.Load", LoadGridCoreAsync, allowRetry: true);

    private async Task LoadGridCoreAsync()
    {
        var version = ++_loadVersion;
        ClearError();

        if (SelectedGroup is not { } group)
        {
            Columns.Clear();
            Rows.Clear();
            HasNoLessons = true;
            return;
        }

        var grid = await monthGridService.GetMonthGridAsync(
            group.StudentGroupId, SelectedSubjectOption?.Value, Year, Month);

        if (version != _loadVersion)
            return;

        var columns = grid.Lessons.Select(l => new GridColumnViewModel(l)).ToList();

        Columns.Clear();
        foreach (var column in columns)
            Columns.Add(column);

        Rows.Clear();
        foreach (var row in grid.Rows)
            Rows.Add(new GridRowViewModel(row, columns));

        HasNoLessons = columns.Count == 0;
    }

    [RelayCommand]
    private Task PreviousMonthAsync()
    {
        var first = new DateOnly(Year, Month, 1).AddMonths(-1);
        Year = first.Year;
        Month = first.Month;
        return LoadGridAsync();
    }

    [RelayCommand]
    private Task NextMonthAsync()
    {
        var first = new DateOnly(Year, Month, 1).AddMonths(1);
        Year = first.Year;
        Month = first.Month;
        return LoadGridAsync();
    }

    [RelayCommand]
    private void OpenCell(GridCellViewModel? cell)
    {
        if (cell is not null)
            OpenLessonRequested?.Invoke(cell.LessonId);
    }
}
