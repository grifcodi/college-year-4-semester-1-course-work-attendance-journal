using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Data.Configurations;

internal class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
   public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.Property(sg => sg.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sg => sg.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.AcademicYear)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(sg => sg.NormalizedName)
            .IsUnique()
            .HasFilter("[IsArchived] = 0");
    }
}
