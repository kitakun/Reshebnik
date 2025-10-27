using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class MetricEmployeeLinkEntityConfiguration : IEntityTypeConfiguration<MetricEmployeeLinkEntity>
{
    public void Configure(EntityTypeBuilder<MetricEmployeeLinkEntity> builder)
    {
        builder.ToTable("metric_employee_links");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.HasIndex(e => new { e.MetricId, e.EmployeeId });

        builder.Property(e => e.MetricId)
            .HasColumnName("metric_id")
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.HasOne(e => e.Metric)
            .WithMany(m => m.EmployeeLinks)
            .HasForeignKey(e => e.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Employee)
            .WithMany(e => e.MetricLinks)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
