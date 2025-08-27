using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class ArchivedMetricEntityConfiguration : IEntityTypeConfiguration<ArchivedMetricEntity>
{
    public void Configure(EntityTypeBuilder<ArchivedMetricEntity> builder)
    {
        builder.ToTable("archived_metrics");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(a => a.MetricId)
            .HasColumnName("metric_id")
            .IsRequired(false);

        builder.Property(a => a.IndicatorId)
            .HasColumnName("indicator_id")
            .IsRequired(false);

        builder.HasOne(a => a.Metric)
            .WithOne(m => m.ArchivedMetric)
            .HasForeignKey<ArchivedMetricEntity>(a => a.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Indicator)
            .WithOne(m => m.ArchivedMetric)
            .HasForeignKey<ArchivedMetricEntity>(a => a.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CompanyEntity>()
            .WithMany(c => c.ArchivedMetrics)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.MetricType)
            .HasColumnName("metric_type")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(a => a.FirstDate)
            .HasColumnName("first_date")
            .IsRequired();

        builder.Property(a => a.LastDate)
            .HasColumnName("last_date")
            .IsRequired();

        builder.Property(a => a.ArchivedAt)
            .HasColumnName("archived_at")
            .IsRequired();

        builder.Property(a => a.ArchivedByUserId)
            .HasColumnName("archived_by_user_id")
            .IsRequired();

        builder.HasOne(a => a.ArchivedByUser)
            .WithMany()
            .HasForeignKey(a => a.ArchivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}