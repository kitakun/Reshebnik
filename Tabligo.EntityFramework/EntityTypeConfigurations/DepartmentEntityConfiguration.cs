using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class DepartmentEntityConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1024);

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(d => d.IsFundamental)
            .HasColumnName("is_fundamental")
            .IsRequired();

        builder.Property(d => d.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(d => d.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasOne(o => o.Company)
            .WithMany(m => m.Departments)
            .HasForeignKey(k => k.CompanyId);

        builder.HasMany(d => d.LinkEntities)
            .WithOne(e => e.Department)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.OwnerSchemas)
            .WithOne(s => s.FundamentalDepartment)
            .HasForeignKey(s => s.FundamentalDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.PartInSchemas)
            .WithOne(s => s.Department)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ignore calced properties
        builder.Ignore(d => d.SupervisorsCalculatedLink);
        builder.Ignore(d => d.EmployeesCalculatedLink);
        
        // Configure ExternalIdLinks navigation
        builder.HasMany(d => d.ExternalIdLinks)
            .WithOne(x => x.Department)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}