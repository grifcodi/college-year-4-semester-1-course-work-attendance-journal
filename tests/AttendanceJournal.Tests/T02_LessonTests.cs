using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Tests;

public class T02_LessonTests : AttendanceTestBase
{
    private static readonly DateOnly Day = new(2026, 11, 10);

    [Fact]
    public async Task Create_StoresLesson_WithTrimmedTopic()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 2, "  Інтеграли  ");

        Assert.True(result.IsSuccess);
        var saved = await Lessons.GetByIdAsync(result.Value!.LessonId);
        Assert.NotNull(saved);
        Assert.Equal(groupId, saved.StudentGroupId);
        Assert.Equal(subjectId, saved.SubjectId);
        Assert.Equal(Day, saved.Date);
        Assert.Equal(2, saved.PairNumber);
        Assert.Equal("Інтеграли", saved.Topic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTopic_IsStoredAsNull(string? topic)
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 1, topic);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Topic);
    }

    [Fact]
    public async Task Create_DuplicateGroupDatePair_IsRejected_WithDateAndPairInMessage()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Lessons.CreateAsync(groupId, subjectId, Day, 3, null);

        var duplicate = await Lessons.CreateAsync(groupId, subjectId, Day, 3, "Інша тема");

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ErrorKeys.LessonDuplicateSlot, duplicate.ErrorKey);
        Assert.Equal(["10.11.2026", 3], duplicate.ErrorArgs);
    }

    [Fact]
    public async Task Create_SameSlotWithDifferentSubject_IsStillRejected()
    {
        var groupId = await CreateGroupAsync();
        var math = await CreateSubjectAsync("Математика");
        var physics = await CreateSubjectAsync("Фізика");
        await Lessons.CreateAsync(groupId, math, Day, 1, null);

        var result = await Lessons.CreateAsync(groupId, physics, Day, 1, null);

        Assert.Equal(ErrorKeys.LessonDuplicateSlot, result.ErrorKey);
    }

    [Fact]
    public async Task Create_SameSlotInAnotherGroup_IsAllowed()
    {
        var group1 = await CreateGroupAsync("ІПЗ-21");
        var group2 = await CreateGroupAsync("ІПЗ-22");
        var subjectId = await CreateSubjectAsync();
        await Lessons.CreateAsync(group1, subjectId, Day, 1, null);

        var result = await Lessons.CreateAsync(group2, subjectId, Day, 1, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_SameDateDifferentPair_IsAllowed()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 2, null);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task Create_PairNumberOutsideRange_IsRejected(int pair)
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, pair, null);

        Assert.Equal(ErrorKeys.LessonInvalidPairNumber, result.ErrorKey);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public async Task Create_PairNumberBoundaries_AreAccepted(int pair)
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, pair, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_TopicLimit_AcceptsMaxAndRejectsOneMore()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var max = ValidationConstants.LessonTopicMaxLength;

        var ok = await Lessons.CreateAsync(groupId, subjectId, Day, 1, new string('а', max));
        var tooLong = await Lessons.CreateAsync(groupId, subjectId, Day, 2, new string('б', max + 1));

        Assert.True(ok.IsSuccess);
        Assert.Equal(ErrorKeys.LessonTopicTooLong, tooLong.ErrorKey);
        Assert.Equal([max], tooLong.ErrorArgs);
    }

    [Fact]
    public async Task Create_ForArchivedGroup_IsRejected()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Groups.ArchiveAsync(groupId);

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);

        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task Create_ForArchivedSubject_IsRejected()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Subjects.ArchiveAsync(subjectId);

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);

        Assert.Equal(ErrorKeys.EntityNotFound, result.ErrorKey);
    }

    [Fact]
    public async Task Create_ForMissingGroupOrSubject_IsRejected()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var noGroup = await Lessons.CreateAsync(999, subjectId, Day, 1, null);
        var noSubject = await Lessons.CreateAsync(groupId, 999, Day, 1, null);

        Assert.Equal(ErrorKeys.EntityNotFound, noGroup.ErrorKey);
        Assert.Equal(ErrorKeys.EntityNotFound, noSubject.ErrorKey);
    }


    [Fact]
    public async Task Create_WritesOneCreateAuditEntry()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var result = await Lessons.CreateAsync(groupId, subjectId, Day, 1, "Тема");

        var entry = Assert.Single(await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, result.Value!.LessonId));
        Assert.Equal(AuditAction.Create, entry.Action);
        Assert.Contains("Тема", entry.PayloadJson);
    }

    [Fact]
    public async Task FailedCreate_WritesNoAuditEntry_AndNoLesson()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);
        var before = (await ContextFactory.GetAuditAsync(AuditEntityType.Lesson)).Count;

        await Lessons.CreateAsync(groupId, subjectId, Day, 1, null); // дублікат
        await Lessons.CreateAsync(groupId, subjectId, Day, 9, null); // поганий номер

        Assert.Equal(before, (await ContextFactory.GetAuditAsync(AuditEntityType.Lesson)).Count);
        await using var context = await ContextFactory.CreateDbContextAsync(Ct);
        Assert.Equal(1, await context.Lessons.CountAsync(Ct));
    }


    [Fact]
    public async Task GetByMonth_ReturnsOnlyThatMonthAndSubject_OrderedByDateThenPair()
    {
        var groupId = await CreateGroupAsync();
        var math = await CreateSubjectAsync("Математика");
        var physics = await CreateSubjectAsync("Фізика");

        await Lessons.CreateAsync(groupId, math, new DateOnly(2026, 10, 31), 1, "жовтень");
        await Lessons.CreateAsync(groupId, math, new DateOnly(2026, 11, 1), 2, "друга");
        await Lessons.CreateAsync(groupId, math, new DateOnly(2026, 11, 1), 1, "перша");
        await Lessons.CreateAsync(groupId, math, new DateOnly(2026, 11, 30), 1, "остання");
        await Lessons.CreateAsync(groupId, math, new DateOnly(2026, 12, 1), 1, "грудень");
        await Lessons.CreateAsync(groupId, physics, new DateOnly(2026, 11, 5), 1, "інший предмет");

        var lessons = await Lessons.GetByMonthAsync(groupId, math, 2026, 11);

        Assert.Equal(["перша", "друга", "остання"], lessons.Select(l => l.Topic));
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(0, 5)]
    public async Task GetByMonth_InvalidMonthOrYear_ReturnsEmpty_InsteadOfThrowing(int year, int month)
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();

        var lessons = await Lessons.GetByMonthAsync(groupId, subjectId, year, month);

        Assert.Empty(lessons);
    }

    [Fact]
    public async Task ArchivedSubject_KeepsItsLessons()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        await Lessons.CreateAsync(groupId, subjectId, Day, 1, "Тема");
        await Subjects.ArchiveAsync(subjectId);

        var lessons = await Lessons.GetByMonthAsync(groupId, subjectId, 2026, 11);

        Assert.Single(lessons);
    }

    [Fact]
    public async Task GetById_ReturnsLesson_OrNullWhenMissing()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var created = await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);

        Assert.NotNull(await Lessons.GetByIdAsync(created.Value!.LessonId));
        Assert.Null(await Lessons.GetByIdAsync(999));
    }


    [Fact]
    public async Task UpdateTopic_ChangesTopic_AndWritesUpdateAuditEntry()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var created = await Lessons.CreateAsync(groupId, subjectId, Day, 1, "Стара");
        var id = created.Value!.LessonId;

        var result = await Lessons.UpdateTopicAsync(id, "  Нова  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Нова", (await Lessons.GetByIdAsync(id))!.Topic);
        var actions = (await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, id)).Select(e => e.Action);
        Assert.Equal([AuditAction.Create, AuditAction.Update], actions);
    }

    [Fact]
    public async Task UpdateTopic_WithSameTopic_WritesNoAuditEntry()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var created = await Lessons.CreateAsync(groupId, subjectId, Day, 1, "Тема");
        var id = created.Value!.LessonId;

        var result = await Lessons.UpdateTopicAsync(id, "Тема");

        Assert.True(result.IsSuccess);
        Assert.Single(await ContextFactory.GetAuditAsync(AuditEntityType.Lesson, id));
    }

    [Fact]
    public async Task UpdateTopic_WithNull_ClearsTopic()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var created = await Lessons.CreateAsync(groupId, subjectId, Day, 1, "Тема");

        await Lessons.UpdateTopicAsync(created.Value!.LessonId, null);

        Assert.Null((await Lessons.GetByIdAsync(created.Value.LessonId))!.Topic);
    }

    [Fact]
    public async Task UpdateTopic_TooLongOrMissingLesson_IsRejected()
    {
        var groupId = await CreateGroupAsync();
        var subjectId = await CreateSubjectAsync();
        var created = await Lessons.CreateAsync(groupId, subjectId, Day, 1, null);

        var tooLong = await Lessons.UpdateTopicAsync(
            created.Value!.LessonId, new string('а', ValidationConstants.LessonTopicMaxLength + 1));
        var missing = await Lessons.UpdateTopicAsync(999, "Тема");

        Assert.Equal(ErrorKeys.LessonTopicTooLong, tooLong.ErrorKey);
        Assert.Equal(ErrorKeys.EntityNotFound, missing.ErrorKey);
    }
}