using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services.Common;

public static class DatabaseErrorClassifier
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintUnique = 2067;

    public static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique };

    public static bool IsBusyOrLocked(Exception ex) =>
        FindSqliteException(ex) is { SqliteErrorCode: SqliteBusy or SqliteLocked };

    public static bool IsDatabaseAccessError(Exception ex) =>
        FindSqliteException(ex) is not null || ex is DbUpdateException;

    public static string? ClassifyErrorKey(Exception ex)
    {
        if (IsBusyOrLocked(ex))
            return ErrorKeys.DatabaseBusy;

        if (ex is DbUpdateConcurrencyException)
            return ErrorKeys.DatabaseConflict;

        // CHECK / FK / unexpected UNIQUE failures are bugs, not an unavailable database:
        // return null so the caller logs the exception and shows the generic error.
        if (FindSqliteException(ex) is { SqliteErrorCode: SqliteConstraint })
            return null;

        if (IsDatabaseAccessError(ex))
            return ErrorKeys.DatabaseUnavailable;

        return null;
    }

    private static SqliteException? FindSqliteException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is SqliteException sqliteEx)
                return sqliteEx;
            ex = ex.InnerException;
        }
        return null;
    }
}


