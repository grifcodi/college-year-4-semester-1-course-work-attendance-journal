namespace AttendanceJournal.ViewModels;

public static class AcademicYearDefaults
{
    public static string For(DateOnly date)
    {
        var startYear = date.Month >= 8 ? date.Year : date.Year - 1;
        return $"{startYear}-{startYear + 1}";
    }

    public static string Current() => For(DateOnly.FromDateTime(DateTime.Today));
}