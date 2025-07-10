using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class IndicatorEntityConfiguration : IEntityTypeConfiguration<IndicatorEntity>
{
    public void Configure(EntityTypeBuilder<IndicatorEntity> builder)
    {
        builder.ToTable("indicators");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(i => i.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Category)
            .HasColumnName("category")
            .HasMaxLength(128);

        builder.Property(i => i.UnitType)
            .HasColumnName("unit_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(i => i.FillmentPeriod)
            .HasColumnName("fillment_period")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(i => i.ValueType)
            .HasColumnName("value_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(i => i.RejectionTreshold)
            .HasColumnName("rejection_treshold");

        builder.Property(i => i.ShowToEmployees)
            .HasColumnName("show_to_employees");

        builder.Property(i => i.ShowOnMainScreen)
            .HasColumnName("show_on_main_screen");

        builder.Property(i => i.ShowOnKeyIndicators)
            .HasColumnName("show_on_key_indicators");

        builder.Property(i => i.EmployeeId).HasColumnName("employee_id");
        builder.Property(i => i.DepartmentId).HasColumnName("department_id");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at");
        builder.Property(i => i.CreatedBy)
            .HasColumnName("created_by");

        builder.HasOne(i => i.Employee)
            .WithMany()
            .HasForeignKey(i => i.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Department)
            .WithMany()
            .HasForeignKey(i => i.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(i => i.CreatedByCompany)
            .WithMany()
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
