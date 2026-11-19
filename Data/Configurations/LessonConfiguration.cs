using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Lesson_PairNumber",
            "[PairNumber] >= 1 AND [PairNumber] <= 8"));

        builder.Property(l => l.Topic)
            .HasMaxLength(ValidationConstants.LessonTopicMaxLength);

        builder.HasIndex(l => new { l.StudentGroupId, l.Date, l.PairNumber })
            .IsUnique();

        builder.HasIndex(l => l.Date);
    }
}
