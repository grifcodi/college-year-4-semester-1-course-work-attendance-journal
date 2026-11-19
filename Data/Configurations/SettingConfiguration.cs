using AttendanceJournal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceJournal.Data.Configurations;

internal class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasData(
            new Setting { Key = "Language", Value = "uk-UA" },
            new Setting { Key = "Theme", Value = "Light" }
        );
    }
}
