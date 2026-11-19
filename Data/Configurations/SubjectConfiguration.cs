using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.Property(s => s.Name)
            .HasMaxLength(ValidationConstants.SubjectNameMaxLength)
            .IsRequired();

        builder.Property(s => s.NormalizedName)
            .HasMaxLength(ValidationConstants.SubjectNameMaxLength)
            .IsRequired();

        builder.HasIndex(s => s.NormalizedName)
            .IsUnique();
    }
}
