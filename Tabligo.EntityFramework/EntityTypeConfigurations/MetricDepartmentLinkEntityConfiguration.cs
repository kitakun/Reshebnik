using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class MetricDepartmentLinkEntityConfiguration : IEntityTypeConfiguration<MetricDepartmentLinkEntity>
{
    public void Configure(EntityTypeBuilder<MetricDepartmentLinkEntity> builder)
    {
        builder.ToTable("metric_department_links");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.HasIndex(e => new { e.MetricId, e.DepartmentId });

        builder.Property(e => e.MetricId)
            .HasColumnName("metric_id")
            .IsRequired();

        builder.Property(e => e.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.HasOne(e => e.Metric)
            .WithMany(m => m.DepartmentLinks)
            .HasForeignKey(e => e.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.MetricLinks)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
