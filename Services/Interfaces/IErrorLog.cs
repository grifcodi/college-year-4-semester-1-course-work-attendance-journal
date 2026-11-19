namespace AttendanceJournal.Services.Interfaces;

public interface IErrorLog
{
    void Log(string context, Exception exception);
}