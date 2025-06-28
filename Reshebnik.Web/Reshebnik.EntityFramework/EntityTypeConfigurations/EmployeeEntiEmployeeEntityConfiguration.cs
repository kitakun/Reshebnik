using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class EmployeeEntityConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasOne(e => e.Company)
            .WithMany(c => c.Employees)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.FIO)
            .HasColumnName("fio")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.JobTitle)
            .HasColumnName("job_title")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(64);

        builder.Property(e => e.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1024);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active");

        builder.Property(e => e.EmailInvitationCode)
            .HasColumnName("email_invitation_code")
            .HasMaxLength(128);
    }
}