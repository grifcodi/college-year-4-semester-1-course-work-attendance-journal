using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class LessonService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), ILessonService
{
    public async Task<List<Lesson>> GetByMonthAsync(int groupId, int? subjectId, int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9998)
            return [];

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await GetByRangeAsync(groupId, startDate, endDate, subjectId);
    }

    public async Task<List<Lesson>> GetByRangeAsync(int groupId, DateOnly fromDate, DateOnly toDate, int? subjectId = null)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var query = context.Lessons
            .AsNoTracking()
            .Include(l => l.Subject)
            .Where(l => l.StudentGroupId == groupId
                        && l.Date >= fromDate
                        && l.Date <= toDate);

        if (subjectId is int sid)
            query = query.Where(l => l.SubjectId == sid);

        return await query
            .OrderBy(l => l.Date)
            .ThenBy(l => l.PairNumber)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdAsync(int lessonId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await context.Lessons
            .AsNoTracking()
            .Include(l => l.Subject)
            .Include(l => l.StudentGroup)
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
            if (oldTopic == normalizedTopic)
                return Result<Lesson>.Success(lesson);

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

    /// <summary>A series may span at most one year (a leap year included).</summary>
    public const int MaxSeriesDays = 366;
    public const int MaxWeekInterval = 4;

    public async Task<Result<LessonSeriesPlan>> PlanSeriesAsync(LessonSeriesRequest request)
    {
        var validation = ValidateSeries(request);
        if (!validation.IsSuccess)
            return Result<LessonSeriesPlan>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await BuildPlanAsync(context, request, validation.Value!);
    }

    public async Task<Result<LessonSeriesResult>> CreateSeriesAsync(LessonSeriesRequest request)
    {
        var validation = ValidateSeries(request);
        if (!validation.IsSuccess)
            return Result<LessonSeriesResult>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var dates = validation.Value!;
        var topic = NormalizeTopic(request.Topic);

        return await ExecuteInTransactionAsync(async context =>
        {
            // Planned again inside the transaction: lessons may have been added since the preview.
            var plan = await BuildPlanAsync(context, request, dates);
            if (!plan.IsSuccess)
                return Result<LessonSeriesResult>.Failure(plan.ErrorKey!, plan.ErrorArgs);

            var lessons = plan.Value!.NewDates
                .Select(date => new Lesson
                {
                    StudentGroupId = request.GroupId,
                    SubjectId = request.SubjectId,
                    Date = date,
                    PairNumber = request.PairNumber,
                    Topic = topic
                })
                .ToList();

            context.Lessons.AddRange(lessons);
            await context.SaveChangesAsync();

            var createdIds = lessons.Select(l => l.LessonId).ToList();

            // One user action = one audit entry (spec 5.1).
            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.CreateSeries,
                AuditEntityType.Lesson,
                null,
                new
                {
                    StudentGroupId = request.GroupId,
                    request.SubjectId,
                    DayOfWeek = request.DayOfWeek.ToString(),
                    request.PairNumber,
                    request.StartDate,
                    request.EndDate,
                    request.WeekInterval,
                    Topic = topic,
                    CreatedLessonIds = createdIds,
                    SkippedDates = plan.Value.TakenDates
                }));

            await context.SaveChangesAsync();
            return Result<LessonSeriesResult>.Success(new LessonSeriesResult(createdIds, plan.Value.TakenDates));
        }, ErrorKeys.LessonSeriesConflict);
    }

    /// <summary>Checks the request and returns every date of the series.</summary>
    internal static Result<List<DateOnly>> ValidateSeries(LessonSeriesRequest request)
    {
        if (request.PairNumber is < 1 or > 8)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonInvalidPairNumber);

        if (NormalizeTopic(request.Topic)?.Length > ValidationConstants.LessonTopicMaxLength)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonTopicTooLong, ValidationConstants.LessonTopicMaxLength);

        if (!Enum.IsDefined(request.DayOfWeek))
            return Result<List<DateOnly>>.Failure(ErrorKeys.UnexpectedError);

        if (request.WeekInterval is < 1 or > MaxWeekInterval)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonSeriesInvalidInterval, MaxWeekInterval);

        if (request.EndDate < request.StartDate)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonSeriesInvalidRange);

        if (request.EndDate.DayNumber - request.StartDate.DayNumber > MaxSeriesDays)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonSeriesTooLong);

        var first = request.StartDate.AddDays(((int)request.DayOfWeek - (int)request.StartDate.DayOfWeek + 7) % 7);
        var dates = new List<DateOnly>();
        for (var date = first; date <= request.EndDate; date = date.AddDays(7 * request.WeekInterval))
            dates.Add(date);

        if (dates.Count == 0)
            return Result<List<DateOnly>>.Failure(ErrorKeys.LessonSeriesNoDates);

        return Result<List<DateOnly>>.Success(dates);
    }

    private static async Task<Result<LessonSeriesPlan>> BuildPlanAsync(
        AppDbContext context,
        LessonSeriesRequest request,
        List<DateOnly> dates)
    {
        var groupExists = await context.StudentGroups
            .AnyAsync(g => g.StudentGroupId == request.GroupId && !g.IsArchived);
        var subjectExists = await context.Subjects
            .AnyAsync(s => s.SubjectId == request.SubjectId && !s.IsArchived);

        if (!groupExists || !subjectExists)
            return Result<LessonSeriesPlan>.Failure(ErrorKeys.EntityNotFound);

        var first = dates[0];
        var last = dates[^1];
        var takenList = await context.Lessons
            .Where(l => l.StudentGroupId == request.GroupId
                        && l.PairNumber == request.PairNumber
                        && l.Date >= first
                        && l.Date <= last)
            .Select(l => l.Date)
            .ToListAsync();
        var taken = takenList.ToHashSet();

        var newDates = dates.Where(d => !taken.Contains(d)).ToList();
        var takenDates = dates.Where(taken.Contains).ToList();

        if (newDates.Count == 0)
            return Result<LessonSeriesPlan>.Failure(ErrorKeys.LessonSeriesAllTaken);

        return Result<LessonSeriesPlan>.Success(new LessonSeriesPlan(newDates, takenDates));
    }

    private static string? NormalizeTopic(string? topic) =>
        string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
}
