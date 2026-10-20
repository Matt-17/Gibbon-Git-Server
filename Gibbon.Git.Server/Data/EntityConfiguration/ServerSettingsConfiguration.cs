using Gibbon.Git.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gibbon.Git.Server.Data.EntityConfiguration;

public class ServerSettingsConfiguration : IEntityTypeConfiguration<ServerSettingsEntity>
{
    public void Configure(EntityTypeBuilder<ServerSettingsEntity> builder)
    {
        builder.ToTable("ServerSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.AllowAnonymousPush)
            .IsRequired();

        builder.Property(s => s.AllowUserRepositoryCreation)
            .IsRequired();

        builder.Property(s => s.AllowAnonymousRegistration)
            .IsRequired();

        builder.Property(s => s.DefaultLanguage)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.SiteTitle)
            .HasMaxLength(100);

        builder.Property(s => s.SiteLogoUrl)
            .HasMaxLength(250);

        builder.Property(s => s.SiteCssUrl)
            .HasMaxLength(250);

        builder.Property(s => s.IsCommitAuthorAvatarVisible)
            .IsRequired();

        builder.Property(s => s.LinksRegex)
            .HasMaxLength(500);

        builder.Property(s => s.LinksUrl)
            .HasMaxLength(250);

        builder.Property(s => s.DisplayNameFormat)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.SortNameFormat)
            .HasConversion<int>()
            .IsRequired();
    }
}
