using AttendanceJournal.Services.Interfaces;

namespace AttendanceJournal.Services.Common;

public sealed class FileErrorLog : IErrorLog
{
    public void Log(string context, Exception exception) =>
        CrashLogger.LogException(context, exception);
}