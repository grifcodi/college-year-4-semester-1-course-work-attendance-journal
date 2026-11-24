using AttendanceJournal.ViewModels.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AttendanceJournal.Views;

/// <summary>Open and save dialogs through Avalonia's storage provider.</summary>
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    public async Task<PickedFile?> PickFileToOpenAsync(string title, string fileTypeName, IReadOnlyList<string> extensions)
    {
        var storage = GetStorageProvider();
        if (storage is null)
            return null;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(fileTypeName) { Patterns = extensions.Select(e => "*." + e).ToList() }
            ]
        });

        var file = files.FirstOrDefault();
        return file is null ? null : new PickedFile(file.Name, file.OpenReadAsync);
    }

    public async Task<PickedFile?> PickFileToSaveAsync(string title, string suggestedFileName, string fileTypeName, string extension)
    {
        var storage = GetStorageProvider();
        if (storage is null)
            return null;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType(fileTypeName) { Patterns = ["*." + extension] }
            ]
        });

        return file is null ? null : new PickedFile(file.Name, file.OpenWriteAsync);
    }

    private static IStorageProvider? GetStorageProvider()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return window is null ? null : TopLevel.GetTopLevel(window)?.StorageProvider;
    }
}
