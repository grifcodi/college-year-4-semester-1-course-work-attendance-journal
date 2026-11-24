using AttendanceJournal.ViewModels.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace AttendanceJournal.Views;

public class DialogConfirmationService : IConfirmationService
{
    public async Task<bool> ConfirmAsync(string message)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null)
            return false;

        var dialog = new Window
        {
            Title = Localizer.Translate(UiTextKeys.ConfirmTitle),
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var yes = new Button { Content = Localizer.Translate(UiTextKeys.ConfirmYes), MinWidth = 90 };
        var no = new Button
        {
            Content = Localizer.Translate(UiTextKeys.ConfirmNo),
            MinWidth = 90,
            IsDefault = true,
            IsCancel = true
        };

        yes.Click += (_, _) => dialog.Close(true);
        no.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { yes, no }
                }
            }
        };

        return await dialog.ShowDialog<bool>(owner);
    }
}