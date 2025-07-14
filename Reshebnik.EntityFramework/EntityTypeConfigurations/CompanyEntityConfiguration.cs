using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class CompanyEntityConfiguration : IEntityTypeConfiguration<CompanyEntity>
{
    public void Configure(EntityTypeBuilder<CompanyEntity> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(p => p.Id).HasDefaultValueSql("nextval('\"companies_id_seq\"')");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Industry)
            .HasColumnName("industry")
            .HasMaxLength(128);

        builder.Property(e => e.EmployeesCount)
            .HasColumnName("employees_count");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(64);

        builder.Property(e => e.NotifyAboutLoweringMetrics)
            .HasColumnName("notify_about_lowering_metrics");

        builder.Property(e => e.NotificationType)
            .HasColumnName("notification_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.LanguageCode)
            .HasColumnName("language_code")
            .IsRequired()
            .HasMaxLength(10);
    }
}