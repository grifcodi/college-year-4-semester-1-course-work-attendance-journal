using System.Text;

namespace AttendanceJournal.Services.Common;

public static class CrashLogger
{
    public const string FileName = "errors.log";
    private const string ProfilePlaceholder = "%USERPROFILE%";
    private const int WriteAttempts = 3;

    private static readonly object WriteLock = new();

    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AttendanceJournal",
        "logs");

    public static void LogException(
        string context,
        Exception exception,
        string? directory = null,
        string? userProfilePath = null,
        long maxBytes = 1_000_000)
    {
        _ = TryLog(context, exception, directory, userProfilePath, maxBytes);
    }

    public static Exception? TryLog(
        string context,
        Exception exception,
        string? directory = null,
        string? userProfilePath = null,
        long maxBytes = 1_000_000)
    {
        try
        {
            var dir = directory ?? DefaultDirectory;
            var profile = userProfilePath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var text = new StringBuilder()
                .Append('[').Append(DateTime.UtcNow.ToString("O")).Append("] ").AppendLine(context)
                .AppendLine(exception.ToString())
                .AppendLine("----")
                .ToString();
            text = RemoveProfilePath(text, profile);

            lock (WriteLock)
            {
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, FileName);

                if (File.Exists(path) && new FileInfo(path).Length > maxBytes)
                    File.Move(path, path + ".old", overwrite: true);

                AppendWithRetry(path, text);
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void AppendWithRetry(string path, string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.AppendAllText(path, text, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < WriteAttempts)
            {
                Thread.Sleep(25 * attempt);
            }
        }
    }

    private static string RemoveProfilePath(string text, string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
            return text;

        return text.Replace(profile, ProfilePlaceholder, StringComparison.OrdinalIgnoreCase);
    }
}