using AttendanceJournal.Models;
using AttendanceJournal.Services;

namespace AttendanceJournal.Tests.TestHelpers;

public abstract class SummaryTestBase : AttendanceTestBase
{
    protected SummaryService Summary => new(ContextFactory);

    protected async Task<int> SeedFixedDatasetAsync()
    {
        var groupId = await CreateGroupAsync("ІПЗ-31");
        var subjectId = await CreateSubjectAsync("Алгоритми та структури даних");

        var s1 = await AddStudentAsync(groupId, "Андрієнко Анна");
        var s2 = await AddStudentAsync(groupId, "Бойко Богдан");
        var s3 = await AddStudentAsync(groupId, "Василенко Віра");
        var s4 = await AddStudentAsync(groupId, "Гриценко Гліб");

        var l1 = await CreateLessonAsync(groupId, subjectId, day: 1);
        var l2 = await CreateLessonAsync(groupId, subjectId, day: 2);
        var l3 = await CreateLessonAsync(groupId, subjectId, day: 3);
        var l4 = await CreateLessonAsync(groupId, subjectId, day: 4);

        // Студент 1: 2 Present, 1 Late, 1 Absent; оцінки: 80, 90, 100 (v1 була 60, v2 стала 100)
        await MarkAsync(l1, s1, AttendanceStatus.Present, grade: 80);
        await MarkAsync(l2, s1, AttendanceStatus.Late, grade: 90);
        await MarkAsync(l3, s1, AttendanceStatus.Present, grade: 60);
        await MarkAsync(l3, s1, AttendanceStatus.Present, grade: 100, reason: "Виправлено оцінку");
        await MarkAsync(l4, s1, AttendanceStatus.Absent);

        // Студент 2: 1 Present, 2 Absent, 1 Excused; оцінки: 70, 75, 80
        await MarkAsync(l1, s2, AttendanceStatus.Present, grade: 70);
        await MarkAsync(l2, s2, AttendanceStatus.Absent, grade: 75);
        await MarkAsync(l3, s2, AttendanceStatus.Absent, grade: 80);
        await MarkAsync(l4, s2, AttendanceStatus.Excused);

        // Студент 3: 2 Excused, 2 Unmarked; 2 оцінки (100, 100) -> менше 3
        await MarkAsync(l1, s3, AttendanceStatus.Excused, grade: 100);
        await MarkAsync(l2, s3, AttendanceStatus.Excused, grade: 100);

        // Студент 4: жодної відмітки й оцінки

        return groupId;
    }
}