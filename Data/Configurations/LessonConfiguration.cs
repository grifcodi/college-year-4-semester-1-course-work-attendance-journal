using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Data.Configurations;

internal class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.Property(l => l.Topic)
            .HasMaxLength(250);

        builder.HasIndex(l => new { l.StudentGroupId, l.Date, l.PairNumber })
            .IsUnique();

        builder.HasIndex(l => l.Date);
    }
}
