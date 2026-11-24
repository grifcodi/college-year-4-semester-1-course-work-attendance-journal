using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AttendanceJournal.ViewModels;

public abstract partial class OperationViewModelBase(IErrorLog errorLog) : ObservableObject
{
    private Func<Task>? _lastFailedAction;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool CanRetry { get; set; }

    protected void ClearError()
    {
        ErrorMessage = null;
        CanRetry = false;
        _lastFailedAction = null;
    }

    protected async Task RunSafeAsync(string context, Func<Task> action, bool allowRetry = false)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            errorLog.Log(context, ex);

            var classifiedKey = DatabaseErrorClassifier.ClassifyErrorKey(ex);
            ErrorMessage = Localizer.TranslateError(
                Result.Failure(classifiedKey ?? ErrorKeys.UnexpectedError));

            if (allowRetry && classifiedKey is not null)
            {
                _lastFailedAction = () => RunSafeAsync(context, action, allowRetry: true);
                CanRetry = true;
            }
            else
            {
                _lastFailedAction = null;
                CanRetry = false;
            }
        }
    }

    [RelayCommand]
    private async Task RetryLastOperationAsync()
    {
        if (_lastFailedAction is null)
            return;

        var retry = _lastFailedAction;
        ClearError();
        await retry();
    }
}