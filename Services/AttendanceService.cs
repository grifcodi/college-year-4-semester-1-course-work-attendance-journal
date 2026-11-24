using AttendanceJournal.Data;
using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Services;

public class AttendanceService(IDbContextFactory<AppDbContext> dbContextFactory)
    : ServiceBase(dbContextFactory), IAttendanceService
{
    public async Task<List<LessonStudentRowDto>> GetLessonSheetAsync(int lessonId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var lesson = await context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LessonId == lessonId);

        if (lesson is null)
            return [];

        var latestMarks = await GetLatestForLessonsInternalAsync(context, [lessonId]);
        var marksByStudent = latestMarks.ToDictionary(m => m.StudentId);
        var markedIds = marksByStudent.Keys.ToList();

        var sheetStudents = await context.Students
            .AsNoTracking()
            .Where(s => s.StudentGroupId == lesson.StudentGroupId
                        && (!s.IsArchived || markedIds.Contains(s.StudentId)))
            .ToListAsync();

        return [.. sheetStudents
            .OrderByName(s => s.FullName)
            .Select(s => new LessonStudentRowDto(
                s.StudentId,
                s.FullName,
                marksByStudent.GetValueOrDefault(s.StudentId)))];
    }

    public async Task<List<AttendanceCellDto>> GetLatestForLessonsAsync(IEnumerable<int> lessonIds)
    {
        var ids = lessonIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await GetLatestForLessonsInternalAsync(context, ids);
    }

    public async Task<List<AttendanceVersion>> GetHistoryAsync(int lessonId, int studentId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await context.AttendanceVersions
            .AsNoTracking()
            .Where(v => v.AttendanceRecord.LessonId == lessonId && v.AttendanceRecord.StudentId == studentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<Result<AttendanceCellDto>> SaveMarkAsync(
        int lessonId,
        int studentId,
        AttendanceStatus status,
        int? grade,
        string? note,
        string? reason)
    {
        var input = new MarkInput(studentId, status, grade, note);
        var validation = ValidateInput(input, reason);
        if (!validation.IsSuccess)
            return Result<AttendanceCellDto>.Failure(validation.ErrorKey!, validation.ErrorArgs);

        var (normalizedNote, normalizedReason) = validation.Value;

        return await ExecuteInTransactionAsync(context =>
            SaveMarkInContextAsync(context, lessonId, input with { Note = normalizedNote }, normalizedReason));
    }

    public async Task<Result<List<AttendanceCellDto>>> SaveMarksAsync(
        int lessonId,
        IReadOnlyList<MarkInput> marks,
        string? reason)
    {
        if (marks.Count == 0)
            return Result<List<AttendanceCellDto>>.Success([]);

        if (marks.Select(m => m.StudentId).Distinct().Count() != marks.Count)
            return Result<List<AttendanceCellDto>>.Failure(ErrorKeys.AttendanceDuplicateStudentInBatch);

        var normalized = new List<MarkInput>(marks.Count);
        string? normalizedReason = null;
        foreach (var mark in marks)
        {
            var validation = ValidateInput(mark, reason);
            if (!validation.IsSuccess)
                return Result<List<AttendanceCellDto>>.Failure(validation.ErrorKey!, validation.ErrorArgs);

            normalized.Add(mark with { Note = validation.Value.Note });
            normalizedReason = validation.Value.Reason;
        }

        // One user action = one transaction (NFR-02): if any mark fails, none is saved.
        return await ExecuteInTransactionAsync(async context =>
        {
            var saved = new List<AttendanceCellDto>(normalized.Count);
            foreach (var mark in normalized)
            {
                var result = await SaveMarkInContextAsync(context, lessonId, mark, normalizedReason);
                if (!result.IsSuccess)
                    return Result<List<AttendanceCellDto>>.Failure(result.ErrorKey!, result.ErrorArgs);

                saved.Add(result.Value!);
            }

            return Result<List<AttendanceCellDto>>.Success(saved);
        });
    }

    private static Result<(string? Note, string? Reason)> ValidateInput(MarkInput input, string? reason)
    {
        if (input.Grade is < 0 or > 100)
            return Result<(string?, string?)>.Failure(ErrorKeys.AttendanceInvalidGrade);

        if (!Enum.IsDefined(input.Status))
            return Result<(string?, string?)>.Failure(ErrorKeys.UnexpectedError);

        var normalizedNote = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        if (normalizedNote?.Length > ValidationConstants.NoteMaxLength)
            return Result<(string?, string?)>.Failure(ErrorKeys.AttendanceNoteTooLong, ValidationConstants.NoteMaxLength);

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedReason?.Length > ValidationConstants.ReasonMaxLength)
            return Result<(string?, string?)>.Failure(ErrorKeys.AttendanceReasonTooLong, ValidationConstants.ReasonMaxLength);

        return Result<(string?, string?)>.Success((normalizedNote, normalizedReason));
    }

    /// <summary>
    /// Appends a version for one student. Runs inside the caller's transaction;
    /// <paramref name="mark"/>.Note and <paramref name="normalizedReason"/> are already trimmed.
    /// </summary>
    private static async Task<Result<AttendanceCellDto>> SaveMarkInContextAsync(
        AppDbContext context,
        int lessonId,
        MarkInput mark,
        string? normalizedReason)
    {
        var (studentId, status, grade, normalizedNote) = mark;

        var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.LessonId == lessonId);
        if (lesson is null)
            return Result<AttendanceCellDto>.Failure(ErrorKeys.EntityNotFound);

        var student = await context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student is null)
            return Result<AttendanceCellDto>.Failure(ErrorKeys.EntityNotFound);

        if (student.StudentGroupId != lesson.StudentGroupId)
            return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceStudentWrongGroup);

        var record = await context.AttendanceRecords
            .Include(r => r.Versions)
            .FirstOrDefaultAsync(r => r.LessonId == lessonId && r.StudentId == studentId);

        if (record is null)
        {
            if (student.IsArchived)
                return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceStudentArchived);

            record = new AttendanceRecord
            {
                LessonId = lessonId,
                StudentId = studentId
            };
            context.AttendanceRecords.Add(record);
            await context.SaveChangesAsync();
        }

        var currentMaxVersion = record.Versions.Count == 0
            ? 0
            : record.Versions.Max(v => v.VersionNumber);

        if (currentMaxVersion > 0)
        {
            var current = record.Versions.First(v => v.VersionNumber == currentMaxVersion);
            if (current.Status == status && current.Grade == grade && current.Note == normalizedNote)
                return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceNoChange);

            if (string.IsNullOrEmpty(normalizedReason))
                return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceReasonRequired);

            if (normalizedReason.Length < ValidationConstants.ReasonMinLength)
                return Result<AttendanceCellDto>.Failure(
                    ErrorKeys.AttendanceReasonTooShort,
                    ValidationConstants.ReasonMinLength);
        }

        var storedReason = currentMaxVersion == 0 ? null : normalizedReason;
        var nextVersionNumber = currentMaxVersion + 1;
        var newVersion = new AttendanceVersion
        {
            AttendanceRecordId = record.AttendanceRecordId,
            VersionNumber = nextVersionNumber,
            Status = status,
            Grade = grade,
            Note = normalizedNote,
            Reason = storedReason,
            TimeStamp = DateTime.UtcNow
        };

        context.AttendanceVersions.Add(newVersion);

        context.AuditEntries.Add(AuditHelper.CreateEntry(
            AuditAction.MarkAttendance,
            AuditEntityType.AttendanceRecord,
            record.AttendanceRecordId,
            new
            {
                LessonId = lessonId,
                StudentId = studentId,
                Version = nextVersionNumber,
                Status = status.ToString(),
                Grade = grade,
                Note = normalizedNote,
                Reason = storedReason
            }));

        await context.SaveChangesAsync();

        return Result<AttendanceCellDto>.Success(new AttendanceCellDto(
            record.AttendanceRecordId,
            lessonId,
            studentId,
            nextVersionNumber,
            status,
            grade,
            normalizedNote));
    }

    public async Task<Result<int>> MarkAllPresentAsync(int lessonId)
    {
        return await ExecuteInTransactionAsync(async context =>
        {
            var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.LessonId == lessonId);
            if (lesson is null)
                return Result<int>.Failure(ErrorKeys.EntityNotFound);

            var activeStudentIds = await context.Students
                .Where(s => s.StudentGroupId == lesson.StudentGroupId && !s.IsArchived)
                .Select(s => s.StudentId)
                .ToListAsync();

            var alreadyMarkedStudentIds = await context.AttendanceRecords
                .Where(r => r.LessonId == lessonId && r.Versions.Any())
                .Select(r => r.StudentId)
                .ToHashSetAsync();

            var unmarkedStudentIds = activeStudentIds
                .Where(id => !alreadyMarkedStudentIds.Contains(id))
                .ToList();

            if (unmarkedStudentIds.Count == 0)
                return Result<int>.Success(0);

            var existingEmptyRecords = await context.AttendanceRecords
                .Where(r => r.LessonId == lessonId && unmarkedStudentIds.Contains(r.StudentId))
                .ToDictionaryAsync(r => r.StudentId);

            var newRecords = new List<AttendanceRecord>();
            foreach (var studentId in unmarkedStudentIds)
            {
                if (!existingEmptyRecords.ContainsKey(studentId))
                {
                    var rec = new AttendanceRecord
                    {
                        LessonId = lessonId,
                        StudentId = studentId
                    };
                    context.AttendanceRecords.Add(rec);
                    newRecords.Add(rec);
                }
            }

            if (newRecords.Count > 0)
                await context.SaveChangesAsync();

            foreach (var rec in newRecords)
                existingEmptyRecords[rec.StudentId] = rec;

            var now = DateTime.UtcNow;
            foreach (var studentId in unmarkedStudentIds)
            {
                var record = existingEmptyRecords[studentId];
                context.AttendanceVersions.Add(new AttendanceVersion
                {
                    AttendanceRecordId = record.AttendanceRecordId,
                    VersionNumber = 1,
                    Status = AttendanceStatus.Present,
                    Grade = null,
                    Note = null,
                    Reason = null,
                    TimeStamp = now
                });
            }

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.MarkAttendance,
                AuditEntityType.Lesson,
                lessonId,
                new
                {
                    BulkMarkAllPresent = true,
                    MarkedCount = unmarkedStudentIds.Count,
                    StudentIds = unmarkedStudentIds
                }));

            await context.SaveChangesAsync();
            return Result<int>.Success(unmarkedStudentIds.Count);
        });
    }

    public async Task<Result<AttendanceCellDto>> RestoreVersionAsync(
        int attendanceRecordId,
        int targetVersionNumber,
        string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (string.IsNullOrEmpty(normalizedReason))
            return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceReasonRequired);

        if (normalizedReason.Length < ValidationConstants.ReasonMinLength)
            return Result<AttendanceCellDto>.Failure(
                ErrorKeys.AttendanceReasonTooShort,
                ValidationConstants.ReasonMinLength);

        if (normalizedReason.Length > ValidationConstants.ReasonMaxLength)
            return Result<AttendanceCellDto>.Failure(
                ErrorKeys.AttendanceReasonTooLong,
                ValidationConstants.ReasonMaxLength);

        return await ExecuteInTransactionAsync(async context =>
        {
            var record = await context.AttendanceRecords
                .Include(r => r.Versions)
                .FirstOrDefaultAsync(r => r.AttendanceRecordId == attendanceRecordId);

            if (record is null)
                return Result<AttendanceCellDto>.Failure(ErrorKeys.EntityNotFound);

            var targetVersion = record.Versions
                .FirstOrDefault(v => v.VersionNumber == targetVersionNumber);

            if (targetVersion is null)
                return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceVersionNotFound);

            var latest = record.Versions.OrderByDescending(v => v.VersionNumber).First();
            if (latest.Status == targetVersion.Status
                && latest.Grade == targetVersion.Grade
                && latest.Note == targetVersion.Note)
                return Result<AttendanceCellDto>.Failure(ErrorKeys.AttendanceNoChange);

            var nextVersionNumber = latest.VersionNumber + 1;
            var restoredVersion = new AttendanceVersion
            {
                AttendanceRecordId = record.AttendanceRecordId,
                VersionNumber = nextVersionNumber,
                Status = targetVersion.Status,
                Grade = targetVersion.Grade,
                Note = targetVersion.Note,
                Reason = normalizedReason,
                TimeStamp = DateTime.UtcNow
            };

            context.AttendanceVersions.Add(restoredVersion);

            context.AuditEntries.Add(AuditHelper.CreateEntry(
                AuditAction.RestoreVersion,
                AuditEntityType.AttendanceRecord,
                record.AttendanceRecordId,
                new
                {
                    record.LessonId,
                    record.StudentId,
                    RestoredFromVersion = targetVersionNumber,
                    NewVersion = nextVersionNumber,
                    Status = restoredVersion.Status.ToString(),
                    restoredVersion.Grade,
                    restoredVersion.Note,
                    Reason = normalizedReason
                }));

            await context.SaveChangesAsync();

            return Result<AttendanceCellDto>.Success(new AttendanceCellDto(
                record.AttendanceRecordId,
                record.LessonId,
                record.StudentId,
                nextVersionNumber,
                restoredVersion.Status,
                restoredVersion.Grade,
                restoredVersion.Note));
        });
    }

    private static async Task<List<AttendanceCellDto>> GetLatestForLessonsInternalAsync(
        AppDbContext context,
        List<int> lessonIds)
    {
        return await context.AttendanceVersions
            .AsNoTracking()
            .Where(v => lessonIds.Contains(v.AttendanceRecord.LessonId))
            .Where(v => v.VersionNumber == context.AttendanceVersions
                .Where(inner => inner.AttendanceRecordId == v.AttendanceRecordId)
                .Max(inner => inner.VersionNumber))
            .Select(v => new AttendanceCellDto(
                v.AttendanceRecordId,
                v.AttendanceRecord.LessonId,
                v.AttendanceRecord.StudentId,
                v.VersionNumber,
                v.Status,
                v.Grade,
                v.Note))
            .ToListAsync();
    }
}
