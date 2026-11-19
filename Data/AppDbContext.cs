using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceVersion> AttendanceVersions => Set<AttendanceVersion>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<Subject> Subjects => Set<Subject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
