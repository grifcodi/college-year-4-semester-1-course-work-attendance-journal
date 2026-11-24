namespace AttendanceJournal.ViewModels.Interfaces;

/// <summary>A page that refreshes its pickers and data each time the user navigates to it.</summary>
public interface IActivatablePage
{
    Task ActivateAsync();
}
