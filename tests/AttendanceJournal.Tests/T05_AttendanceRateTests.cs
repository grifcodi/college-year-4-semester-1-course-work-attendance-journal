using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T05_AttendanceRateTests : SummaryTestBase
{
    [Fact]
    public async Task AttendanceRate_ExcludesExcusedAndUnmarked_AndHandlesEmptyDenominator()
    {
        var groupId = await SeedFixedDatasetAsync();

        var summary = await Summary.GetGroupSummaryAsync(groupId);

        // Студент 1: (2 + 1) / (2 + 1 + 1) = 75.0%
        var s1 = summary.Students.Single(s => s.FullName == "Андрієнко Анна");
        Assert.Equal(2, s1.PresentCount);
        Assert.Equal(1, s1.LateCount);
        Assert.Equal(1, s1.AbsentCount);
        Assert.Equal(0, s1.ExcusedCount);
        Assert.Equal(0, s1.UnmarkedCount);
        Assert.Equal(75.0, s1.AttendanceRatePercent);

        // Студент 2: (1 + 0) / (1 + 0 + 2) = 33.33%
        var s2 = summary.Students.Single(s => s.FullName == "Бойко Богдан");
        Assert.Equal(33.33, s2.AttendanceRatePercent);

        // Студент 3: 2 Excused, 2 Unmarked -> знаменник = 0 -> null
        var s3 = summary.Students.Single(s => s.FullName == "Василенко Віра");
        Assert.Equal(2, s3.ExcusedCount);
        Assert.Equal(2, s3.UnmarkedCount);
        Assert.Null(s3.AttendanceRatePercent);

        // Студент 4: 0 відміток -> знаменник = 0 -> null
        var s4 = summary.Students.Single(s => s.FullName == "Гриценко Гліб");
        Assert.Equal(4, s4.UnmarkedCount);
        Assert.Null(s4.AttendanceRatePercent);

        // Загальний показник по групі: (3 + 1) / (4 + 3) = 57.14%
        Assert.Equal(57.14, summary.GroupAttendanceRatePercent);
    }

    [Fact]
    public async Task AttendanceRate_WhenNoLessonsExist_ReturnsNullRates()
    {
        var groupId = await CreateGroupAsync();
        await AddStudentAsync(groupId);

        var summary = await Summary.GetGroupSummaryAsync(groupId);

        Assert.Equal(0, summary.TotalLessonsCount);
        Assert.Null(summary.GroupAttendanceRatePercent);
        Assert.Null(Assert.Single(summary.Students).AttendanceRatePercent);
    }
}