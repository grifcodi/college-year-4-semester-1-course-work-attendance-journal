using AttendanceJournal.ViewModels;

namespace AttendanceJournal.Tests;

public class T19_AcademicYearDefaultsTests
{
    [Theory]
    [InlineData(2026, 11, 18, "2026-2027")]
    [InlineData(2026, 9, 1, "2026-2027")]
    [InlineData(2026, 8, 1, "2026-2027")]
    [InlineData(2027, 3, 1, "2026-2027")]
    [InlineData(2027, 7, 31, "2026-2027")]
    [InlineData(2027, 8, 1, "2027-2028")]
    [InlineData(2027, 1, 1, "2026-2027")]
    public void For_ReturnsAcademicYearContainingTheDate(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, AcademicYearDefaults.For(new DateOnly(year, month, day)));
    }
}