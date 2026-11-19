using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class LessonService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), ILessonService
{
    public async Task<List<Lesson>> GetByMonthAsync(int groupId, int subjectId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await context.Lessons
            .AsNoTracking()
            .Where(l => l.StudentGroupId == groupId
                        && l.SubjectId == subjectId
                        && l.Date >= startDate
                        && l.Date <= endDate)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.PairNumber)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdAsync(int lessonId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LessonId == lessonId);
    }

    public async Task<Result<Lesson>> CreateAsync(
        int groupId,
        int subjectId,
        DateOnly date,
        int pairNumber,
        string? topic)
    {
        if (pairNumber is < 1 or > 8)
            return Result<Lesson>.Failure(ErrorKeys.LessonInvalidPairNumber);

        var normalizedTopic = NormalizeTopic(topic);
        if (normalizedTopic?.Length > ValidationConstants.LessonTopicMaxLength)
            return Result<Lesson>.Failure(ErrorKeys.LessonTopicTooLong, ValidationConstants.LessonTopicMaxLength);

        return await ExecuteInTransactionAsync(async context =>
        {
            var groupExists = await context.StudentGroups
                .AnyAsync(g => g.StudentGroupId == groupId && !g.IsArchived);
            var subjectExists = await context.Subjects
                .AnyAsync(s => s.SubjectId == subjectId && !s.IsArchived);

            if (!groupExists || !subjectExists)
                return Result<Lesson>.Failure(ErrorKeys.EntityNotFound);

            var slotTaken = await context.Lessons.AnyAsync(l =>
                l.StudentGroupId == groupId &&
                l.Date == date &&
                l.PairNumber == pairNumber);

            if (slotTaken)
                return Result<Lesson>.Failure(
                    ErrorKeys.LessonDuplicateSlot,
                    date.ToString("dd.MM.yyyy"),
                    pairNumber);

            var lesson = new Lesson
            {
                StudentGroupId = groupId,
                SubjectId = subjectId,
                Date = date,
                PairNumber = pairNumber,
                Topic = normalizedTopic
            };

            context.Lessons.Add(lesson);
            await context.SaveChangesAsync();

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Create,
                AuditEntityType.Lesson,
                lesson.LessonId,
                new
                {
                    lesson.StudentGroupId,
                    lesson.SubjectId,
                    lesson.Date,
                    lesson.PairNumber,
                    lesson.Topic
                }));

            await context.SaveChangesAsync();
            return Result<Lesson>.Success(lesson);
        }, ErrorKeys.LessonDuplicateSlot, date.ToString("dd.MM.yyyy"), pairNumber);
    }

    public async Task<Result<Lesson>> UpdateTopicAsync(int lessonId, string? topic)
    {
        var normalizedTopic = NormalizeTopic(topic);
        if (normalizedTopic?.Length > ValidationConstants.LessonTopicMaxLength)
            return Result<Lesson>.Failure(ErrorKeys.LessonTopicTooLong, ValidationConstants.LessonTopicMaxLength);

        return await ExecuteInTransactionAsync(async context =>
        {
            var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.LessonId == lessonId);
            if (lesson is null)
                return Result<Lesson>.Failure(ErrorKeys.EntityNotFound);

            var oldTopic = lesson.Topic;
            lesson.Topic = normalizedTopic;

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.Update,
                AuditEntityType.Lesson,
                lesson.LessonId,
                new { OldTopic = oldTopic, NewTopic = lesson.Topic }));

            await context.SaveChangesAsync();
            return Result<Lesson>.Success(lesson);
        });
    }

    private static string? NormalizeTopic(string? topic) =>
        string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
}