namespace AttendanceJournal.ViewModels.Interfaces;

public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string message);
}