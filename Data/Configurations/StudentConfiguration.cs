using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.Property(s => s.FullName)
            .HasMaxLength(ValidationConstants.StudentFullNameMaxLength)
            .IsRequired();

        builder.HasIndex(s => new { s.StudentGroupId, s.IsArchived, s.FullName });
    }
}
