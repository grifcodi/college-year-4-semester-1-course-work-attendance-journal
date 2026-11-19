using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AttendanceJournal.ViewModels;

public abstract partial class OperationViewModelBase(IErrorLog errorLog) : ObservableObject
{
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }
    protected async Task RunSafeAsync(string context, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            errorLog.Log(context, ex);
            ErrorMessage = Localizer.TranslateError(Result.Failure(ErrorKeys.UnexpectedError));
        }
    }
}
