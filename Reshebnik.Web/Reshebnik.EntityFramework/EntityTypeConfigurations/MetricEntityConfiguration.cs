using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class MetricEntityConfiguration : IEntityTypeConfiguration<MetricEntity>
{
    public void Configure(EntityTypeBuilder<MetricEntity> builder)
    {
        builder.ToTable("metrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(m => m.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(m => m.EmployeeId)
            .HasColumnName("employee_id");

        builder.HasOne(m => m.Company)
            .WithMany(c => c.Metrics)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Department)
            .WithMany(d => d.Metrics)
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Employee)
            .WithMany(e => e.Metrics)
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

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

        builder.Property(m => m.ClickHouseKey)
            .HasColumnName("clickhouse_key")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(m => m.Plan)
            .HasColumnName("plan_value");

        builder.Property(m => m.Min)
            .HasColumnName("min_value");

        builder.Property(m => m.Max)
            .HasColumnName("max_value");

        builder.Property(m => m.Visible)
            .HasColumnName("visible")
            .IsRequired();
    }
}
