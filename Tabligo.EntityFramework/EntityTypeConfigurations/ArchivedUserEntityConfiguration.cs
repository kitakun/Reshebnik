using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class ArchivedUserEntityConfiguration : IEntityTypeConfiguration<ArchivedUserEntity>
{
    public void Configure(EntityTypeBuilder<ArchivedUserEntity> builder)
    {
        builder.ToTable("archived_users");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(a => a.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.HasOne(a => a.Employee)
            .WithOne(e => e.ArchivedUser)
            .HasForeignKey<ArchivedUserEntity>(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CompanyEntity>()
            .WithMany(c => c.ArchivedUsers)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

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
