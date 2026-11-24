using CommunityToolkit.Mvvm.ComponentModel;

namespace AttendanceJournal.ViewModels;

/// <summary>
/// Base of all view models. Pages that call services derive from <see cref="OperationViewModelBase"/>,
/// which adds the error banner, the info banner and Retry on top of this class.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
