using AttendanceJournal.Models;
using AttendanceJournal.ViewModels.Localization;
using System.Globalization;

namespace AttendanceJournal.ViewModels;

/// <summary>One version of a mark in the history panel (FR-07, FR-16).</summary>
public sealed class HistoryItemViewModel(AttendanceVersion version, bool isCurrent)
{
    public int VersionNumber { get; } = version.VersionNumber;
    public bool IsCurrent { get; } = isCurrent;
    public bool CanRestore => !IsCurrent;

    public string Title { get; } = Localizer.Translate(UiTextKeys.HistoryVersion, version.VersionNumber);
    public string CurrentBadge { get; } = isCurrent ? Localizer.Translate(UiTextKeys.CurrentVersionBadge) : string.Empty;
    public string TimeText { get; } = DisplayText.LocalTime(version.TimeStamp);

    public string Details { get; } = string.Join(" · ", new[]
    {
        DisplayText.StatusName(version.Status),
        version.Grade?.ToString(CultureInfo.InvariantCulture),
        version.Note,
    }.Where(s => !string.IsNullOrEmpty(s)));

    public string ReasonText { get; } = string.IsNullOrEmpty(version.Reason)
        ? string.Empty
        : Localizer.Translate(UiTextKeys.HistoryReason, version.Reason);
}
