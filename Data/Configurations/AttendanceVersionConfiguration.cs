using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Data.Configurations;

internal class AttendanceVersionConfiguration : IEntityTypeConfiguration<AttendanceVersion>
{
    public void Configure(EntityTypeBuilder<AttendanceVersion> builder)
    {
        builder.Property(av => av.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(av => av.Note)
            .HasMaxLength(500);

        builder.Property(av => av.Reason)
            .HasMaxLength(500);

        builder.HasIndex(av => new { av.AttendanceRecordId, av.VersionNumber })
            .IsUnique();
    }
}
