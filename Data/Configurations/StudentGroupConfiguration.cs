using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.Property(sg => sg.Name)
            .HasMaxLength(ValidationConstants.GroupNameMaxLength)
            .IsRequired();

        builder.Property(sg => sg.NormalizedName)
            .HasMaxLength(ValidationConstants.GroupNameMaxLength)
            .IsRequired();

        builder.Property(g => g.AcademicYear)
            .HasMaxLength(ValidationConstants.AcademicYearMaxLength)
            .IsRequired();

        builder.HasIndex(sg => sg.NormalizedName)
            .IsUnique()
            .HasFilter("[IsArchived] = 0");
    }
}
