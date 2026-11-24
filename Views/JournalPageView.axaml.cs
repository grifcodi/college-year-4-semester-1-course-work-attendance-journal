using AttendanceJournal.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AttendanceJournal.Views;

public partial class JournalPageView : UserControl
{
    public JournalPageView()
    {
        InitializeComponent();

        // Tunnel, so the shortcuts work even when a text box has focus (NFR-03).
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not JournalPageViewModel vm || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        switch (e.Key)
        {
            case Key.S:
                if (vm.SaveMarksCommand.CanExecute(null))
                    vm.SaveMarksCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.M:
                if (vm.MarkAllPresentCommand.CanExecute(null))
                    vm.MarkAllPresentCommand.Execute(null);
                e.Handled = true;
                break;

            default:
                var number = e.Key switch
                {
                    Key.D1 or Key.NumPad1 => 1,
                    Key.D2 or Key.NumPad2 => 2,
                    Key.D3 or Key.NumPad3 => 3,
                    Key.D4 or Key.NumPad4 => 4,
                    _ => 0,
                };

                // The focused control sits inside a student row, so its DataContext is that row.
                if (number > 0 && (e.Source as Control)?.DataContext is MarkRowViewModel row)
                {
                    JournalPageViewModel.SetStatusByShortcut(row, number);
                    e.Handled = true;
                }
                break;
        }
    }
}
