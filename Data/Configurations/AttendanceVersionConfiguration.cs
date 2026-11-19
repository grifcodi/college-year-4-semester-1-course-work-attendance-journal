using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class AttendanceVersionConfiguration : IEntityTypeConfiguration<AttendanceVersion>
{
    public void Configure(EntityTypeBuilder<AttendanceVersion> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ATtendanceVersions_Grade",
            "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)"));

        builder.Property(av => av.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(av => av.Note)
            .HasMaxLength(ValidationConstants.NoteMaxLength);

        builder.Property(av => av.Reason)
            .HasMaxLength(ValidationConstants.ReasonMaxLength);

        builder.HasIndex(av => new { av.AttendanceRecordId, av.VersionNumber })
            .IsUnique();
    }
}
