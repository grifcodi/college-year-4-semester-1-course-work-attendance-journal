using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T06_AverageGradeTests : SummaryTestBase
{
    [Fact]
    public async Task AverageGrade_RequiresAtLeastThreeGrades_AndGroupAverageUsesOnlyQualifyingStudents()
    {
        var groupId = await SeedFixedDatasetAsync();

        var summary = await Summary.GetGroupSummaryAsync(groupId);

        // Студент 1: (80 + 90 + 100) / 3 = 90.0 (враховує версію 2 замість 60)
        var s1 = summary.Students.Single(s => s.FullName == "Андрієнко Анна");
        Assert.Equal(3, s1.GradedLessonsCount);
        Assert.Equal(90.0, s1.AverageGrade);

        // Студент 2: (70 + 75 + 80) / 3 = 75.0
        var s2 = summary.Students.Single(s => s.FullName == "Бойко Богдан");
        Assert.Equal(3, s2.GradedLessonsCount);
        Assert.Equal(75.0, s2.AverageGrade);

        // Студент 3: лише 2 оцінки -> null
        var s3 = summary.Students.Single(s => s.FullName == "Василенко Віра");
        Assert.Equal(2, s3.GradedLessonsCount);
        Assert.Null(s3.AverageGrade);

        // Студент 4: 0 оцінок -> null
        var s4 = summary.Students.Single(s => s.FullName == "Гриценко Гліб");
        Assert.Null(s4.AverageGrade);

        // Середній бал групи: (90.0 + 75.0) / 2 = 82.5
        Assert.Equal(82.5, summary.GroupAverageGrade);
    }
}
