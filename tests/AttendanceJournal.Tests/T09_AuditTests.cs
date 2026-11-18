using AttendanceJournal.Models;
using AttendanceJournal.Services;
using AttendanceJournal.Tests.TestHelpers;

namespace AttendanceJournal.Tests;

public class T09_AuditTests : SqliteInMemoryTestBase
{
    private SubjectService Subjects() => new(ContextFactory);
    private StudentGroupService Groups() => new(ContextFactory);
    private StudentService Students() => new(ContextFactory);

    [Fact]
    public async Task Subject_Lifecycle_WritesOneEntryPerOperation_InOrder()
    {
        var service = Subjects();
        var created = await service.CreateAsync("Фізика");
        var id = created.Value!.SubjectId;

        await service.UpdateAsync(id, "Астрономія");
        await service.ArchiveAsync(id);
        await service.UnarchiveAsync(id);

        var entries = await ContextFactory.GetAuditAsync(AuditEntityType.Subject, id);
        Assert.Equal(
            [AuditAction.Create, AuditAction.Update, AuditAction.Archive, AuditAction.Unarchive],
            entries.Select(e => e.Action));
    }

    [Fact]
    public async Task Group_Lifecycle_WritesOneEntryPerOperation_InOrder()
    {
        var service = Groups();
        var created = await service.CreateAsync("ІПЗ-21", "2026-2027");
        var id = created.Value!.StudentGroupId;

        await service.UpdateAsync(id, "ІПЗ-22", "2026-2027");
        await service.ArchiveAsync(id);

        var entries = await ContextFactory.GetAuditAsync(AuditEntityType.StudentGroup, id);
        Assert.Equal(
            [AuditAction.Create, AuditAction.Update, AuditAction.Archive],
            entries.Select(e => e.Action));
    }

    [Fact]
    public async Task Student_Lifecycle_WritesOneEntryPerOperation_InOrder()
    {
        var group = await Groups().CreateAsync("ІПЗ-21", "2026-2027");
        var service = Students();
        var student = await service.AddAsync(group.Value!.StudentGroupId, "Шевченко Тарас");
        var id = student.Value!.StudentId;

        await service.UpdateAsync(id, "Франко Іван");
        await service.ArchiveAsync(id);

        var entries = await ContextFactory.GetAuditAsync(AuditEntityType.Student, id);
        Assert.Equal(
            [AuditAction.Create, AuditAction.Update, AuditAction.Archive],
            entries.Select(e => e.Action));
    }

    [Fact]
    public async Task EntryIds_AreIncreasing_AcrossDifferentEntities()
    {
        await Subjects().CreateAsync("Фізика");
        await Groups().CreateAsync("ІПЗ-21", "2026-2027");
        await Subjects().CreateAsync("Хімія");

        var entries = await ContextFactory.GetAuditAsync();

        Assert.Equal(3, entries.Count);
        Assert.Equal(
            [AuditEntityType.Subject, AuditEntityType.StudentGroup, AuditEntityType.Subject],
            entries.Select(e => e.EntityType));
        Assert.True(entries[0].AuditEntryId < entries[1].AuditEntryId);
        Assert.True(entries[1].AuditEntryId < entries[2].AuditEntryId);
    }

    [Fact]
    public async Task FailedOperations_WriteNoAuditEntries()
    {
        var subjects = Subjects();
        await subjects.CreateAsync("Фізика");
        var before = (await ContextFactory.GetAuditAsync()).Count;

        await subjects.CreateAsync("ФІЗИКА");   
        await subjects.CreateAsync("   ");         
        await subjects.UpdateAsync(999, "Хімія"); 
        await subjects.ArchiveAsync(999);          

        Assert.Equal(before, (await ContextFactory.GetAuditAsync()).Count);
    }

    [Fact]
    public async Task NoOpOperations_WriteNoAuditEntries()
    {
        var subjects = Subjects();
        var created = await subjects.CreateAsync("Фізика");
        var id = created.Value!.SubjectId;
        await subjects.ArchiveAsync(id);
        var before = (await ContextFactory.GetAuditAsync()).Count;

        await subjects.UpdateAsync(id, "Фізика");  
        await subjects.ArchiveAsync(id);          

        Assert.Equal(before, (await ContextFactory.GetAuditAsync()).Count);
    }

    [Fact]
    public async Task Payload_KeepsUkrainianTextReadable()
    {
        await Subjects().CreateAsync("Математика");

        var entry = Assert.Single(await ContextFactory.GetAuditAsync());

        Assert.Contains("Математика", entry.PayloadJson);
        Assert.DoesNotContain("\\u04", entry.PayloadJson);
    }

    [Fact]
    public async Task Entry_HasEntityId_AndUtcTimestampNearNow()
    {
        var created = await Subjects().CreateAsync("Математика");

        var entry = Assert.Single(await ContextFactory.GetAuditAsync());

        Assert.Equal(created.Value!.SubjectId, entry.EntityId);
        Assert.InRange(entry.TimeStamp, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddSeconds(5));
    }
}