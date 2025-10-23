using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class EmployeeDepartmentLinkEntityConfiguration : IEntityTypeConfiguration<EmployeeDepartmentLinkEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentLinkEntity> builder)
    {
        builder.ToTable("employee_department_links");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.HasIndex(i => new { i.DepartmentId, i.EmployeeId });

        builder.Property(e => e.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(e => e.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(e => e.Employee)
            .WithMany(m => m.DepartmentLinks)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
            .WithMany(m => m.LinkEntities)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.Employee.IsArchived);
    }
}