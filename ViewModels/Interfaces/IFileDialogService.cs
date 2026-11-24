namespace AttendanceJournal.ViewModels.Interfaces;

/// <summary>A file chosen by the user. The stream is opened only when needed.</summary>
public sealed record PickedFile(string Name, Func<Task<Stream>> OpenAsync);

/// <summary>
/// Open and save dialogs. View models use this interface; the Avalonia implementation lives in Views/,
/// so view models stay free of UI types and can be tested with a fake.
/// </summary>
public interface IFileDialogService
{
    /// <param name="extensions">Extensions without the dot, for example "csv".</param>
    Task<PickedFile?> PickFileToOpenAsync(string title, string fileTypeName, IReadOnlyList<string> extensions);

    Task<PickedFile?> PickFileToSaveAsync(string title, string suggestedFileName, string fileTypeName, string extension);
}
