using AttendanceJournal.Models;
using AttendanceJournal.ViewModels.Interfaces;
using Avalonia;
using Avalonia.Styling;

namespace AttendanceJournal.Views;

public sealed class AvaloniaThemeApplier : IThemeApplier
{
    public void Apply(AppTheme theme)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}