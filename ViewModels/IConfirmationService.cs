namespace AttendanceJournal.ViewModels;

public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string message);
}