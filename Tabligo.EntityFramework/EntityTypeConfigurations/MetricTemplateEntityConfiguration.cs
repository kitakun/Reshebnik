using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class MetricTemplateEntityConfiguration : IEntityTypeConfiguration<MetricTemplateEntity>
{
    public void Configure(EntityTypeBuilder<MetricTemplateEntity> builder)
    {
        builder.ToTable("metric_templates");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasOne(m => m.Company)
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(m => m.Unit)
            .HasColumnName("unit")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(m => m.PeriodType)
            .HasColumnName("period_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(m => m.WeekType)
            .HasColumnName("week_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(m => m.WeekStartDate)
            .HasColumnName("week_start_date");

        builder.Property(m => m.ShowGrowthPercent)
            .HasColumnName("show_growth_percent")
            .IsRequired();

        builder.Property(m => m.ClickHouseKey)
            .HasColumnName("clickhouse_key")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(m => m.Plan).HasColumnName("plan_value");
        builder.Property(m => m.Min).HasColumnName("min_value");
        builder.Property(m => m.Max).HasColumnName("max_value");
        builder.Property(m => m.Visible).HasColumnName("visible");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
    }
}
