using System.ComponentModel;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.ViewModels.Localization;

public sealed class LocalizationSource : INotifyPropertyChanged
{
    public static LocalizationSource Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Localizer.Translate(key);

    public void NotifyLanguageChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}


