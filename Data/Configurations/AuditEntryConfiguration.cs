using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Data.Configurations;

internal class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.Property(ae => ae.Action)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(ae => ae.EntityType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(ae => ae.PayloadJson)
            .IsRequired();

        builder.HasIndex(ae => ae.TimeStamp);
        builder.HasIndex(ae => new { ae.EntityType, ae.Action });
    }
}
