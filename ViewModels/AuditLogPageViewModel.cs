using AttendanceJournal.Models;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AttendanceJournal.ViewModels;

/// <summary>A line of the change log, formatted for display.</summary>
public sealed class AuditRowViewModel(AuditEntryDto entry)
{
    public int Id { get; } = entry.AuditEntryId;
    public string TimeText { get; } = DisplayText.LocalTime(entry.TimeStampUtc);
    public string ActionText { get; } = DisplayText.ActionName(entry.Action);
    public string EntityText { get; } = DisplayText.EntityName(entry.EntityType);
    public string EntityIdText { get; } = entry.EntityId?.ToString() ?? string.Empty;
    public string Details { get; } = entry.PayloadJson;
}

/// <summary>Read-only viewer of the audit log with filters by date, object type and action (FR-12).</summary>
public partial class AuditLogPageViewModel(
    IAuditLogService auditLogService,
    IErrorLog errorLog) : OperationViewModelBase(errorLog), IActivatablePage
{
    public const int PageSize = 500;

    private bool _suppressReload;
    private int _loadVersion;

    public ObservableCollection<SelectOption<AuditEntityType?>> EntityTypeOptions { get; } = [];
    public ObservableCollection<SelectOption<AuditAction?>> ActionOptions { get; } = [];
    public ObservableCollection<AuditRowViewModel> Entries { get; } = [];

    [ObservableProperty]
    public partial DateTime? FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime? ToDate { get; set; }

    [ObservableProperty]
    public partial SelectOption<AuditEntityType?>? SelectedEntityType { get; set; }

    [ObservableProperty]
    public partial SelectOption<AuditAction?>? SelectedAction { get; set; }

    [ObservableProperty]
    public partial string ShownText { get; set; } = string.Empty;

    partial void OnFromDateChanged(DateTime? value) => ReloadIfAllowed();
    partial void OnToDateChanged(DateTime? value) => ReloadIfAllowed();
    partial void OnSelectedEntityTypeChanged(SelectOption<AuditEntityType?>? value) => ReloadIfAllowed();
    partial void OnSelectedActionChanged(SelectOption<AuditAction?>? value) => ReloadIfAllowed();

    private void ReloadIfAllowed()
    {
        if (!_suppressReload)
            _ = LoadAsync();
    }

    public Task ActivateAsync()
    {
        // Rebuild the option texts, so they follow the current language.
        _suppressReload = true;
        try
        {
            var entity = SelectedEntityType?.Value;
            EntityTypeOptions.Clear();
            EntityTypeOptions.Add(new SelectOption<AuditEntityType?>(null, Localizer.Translate(UiTextKeys.AllEntityTypes)));
            foreach (var type in Enum.GetValues<AuditEntityType>())
                EntityTypeOptions.Add(new SelectOption<AuditEntityType?>(type, DisplayText.EntityName(type)));
            SelectedEntityType = EntityTypeOptions.First(o => o.Value == entity);

            var action = SelectedAction?.Value;
            ActionOptions.Clear();
            ActionOptions.Add(new SelectOption<AuditAction?>(null, Localizer.Translate(UiTextKeys.AllActions)));
            foreach (var a in Enum.GetValues<AuditAction>())
                ActionOptions.Add(new SelectOption<AuditAction?>(a, DisplayText.ActionName(a)));
            SelectedAction = ActionOptions.First(o => o.Value == action);
        }
        finally
        {
            _suppressReload = false;
        }

        return LoadAsync();
    }

    [RelayCommand]
    public Task LoadAsync() => RunSafeAsync("AuditLog.Load", async () =>
    {
        var version = ++_loadVersion;
        ClearError();

        var filter = new AuditLogFilter(
            FromDate is DateTime f ? DateOnly.FromDateTime(f) : null,
            ToDate is DateTime t ? DateOnly.FromDateTime(t) : null,
            SelectedEntityType?.Value,
            SelectedAction?.Value,
            PageSize);

        var page = await auditLogService.GetEntriesAsync(filter);
        if (version != _loadVersion)
            return;

        Entries.Clear();
        foreach (var entry in page.Entries)
            Entries.Add(new AuditRowViewModel(entry));

        ShownText = Localizer.Translate(UiTextKeys.AuditShownCount, page.Entries.Count, page.TotalCount);
    }, allowRetry: true);

    [RelayCommand]
    private void ClearFilters()
    {
        _suppressReload = true;
        try
        {
            FromDate = null;
            ToDate = null;
            SelectedEntityType = EntityTypeOptions.FirstOrDefault();
            SelectedAction = ActionOptions.FirstOrDefault();
        }
        finally
        {
            _suppressReload = false;
        }

        _ = LoadAsync();
    }
}
