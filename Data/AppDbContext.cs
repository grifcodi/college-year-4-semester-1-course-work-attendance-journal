using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceJournal.Data;

internal class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

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
