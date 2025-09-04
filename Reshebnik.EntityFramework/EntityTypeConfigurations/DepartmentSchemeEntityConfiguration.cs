using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class DepartmentSchemeEntityConfiguration : IEntityTypeConfiguration<DepartmentSchemeEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentSchemeEntity> builder)
    {
        builder.ToTable("department_scheme");

        builder.HasKey(e => e.Id);

        builder.HasIndex(i => new { i.FundamentalDepartmentId, i.DepartmentId, i.AncestorDepartmentId });

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.FundamentalDepartmentId)
            .HasColumnName("fundamental_id")
            .IsRequired();

        builder.Property(e => e.AncestorDepartmentId)
            .HasColumnName("ancestor_id")
            .IsRequired();

        builder.Property(e => e.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(e => e.Depth)
            .HasColumnName("depth")
            .IsRequired();

        builder.HasOne(e => e.FundamentalDepartment)
            .WithMany(m => m.OwnerSchemas)
            .HasForeignKey(e => e.FundamentalDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AncestorDepartment)
            .WithMany()
            .HasForeignKey(e => e.AncestorDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany(m => m.PartInSchemas)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}