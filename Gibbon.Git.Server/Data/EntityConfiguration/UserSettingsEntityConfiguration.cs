using Gibbon.Git.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gibbon.Git.Server.Data.EntityConfiguration;

public class UserSettingsEntityConfiguration : IEntityTypeConfiguration<UserSettingsEntity>
{
    public void Configure(EntityTypeBuilder<UserSettingsEntity> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(e => e.UserId);

        builder.Property(e => e.UserId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.PreferredLanguage)
            .HasMaxLength(10);

        builder.Property(e => e.PreferredThemeMode)
            .HasConversion<int>();

        builder.Property(e => e.ReceiveEmailNotifications)
            .IsRequired();

        builder.Property(e => e.TimeZone)
            .HasMaxLength(50);

        builder.Property(e => e.DateFormat)
            .HasMaxLength(20);

        builder.Property(e => e.DefaultHomePage)
            .HasMaxLength(100);

        builder.Property(e => e.DisplayNameFormat)
            .HasConversion<int?>();

        builder.Property(e => e.SortNameFormat)
            .HasConversion<int?>();
    }
}
