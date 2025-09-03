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
        builder.HasIndex(e => e.Email);

        builder.Property(p => p.Id).HasDefaultValueSql("nextval('\"employee_id_seq\"')");

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
            .HasMaxLength(256);

        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(64);

        builder.Property(e => e.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1024);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active");

        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived");

        builder.Property(e => e.EmailInvitationCode)
            .HasColumnName("email_invitation_code")
            .HasMaxLength(128);

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.DefaultRole)
            .HasColumnName("default_role")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(e => e.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(e => e.WelcomeWasSeen)
            .HasColumnName("welcome_was_seen");

        builder.Property(p => p.Salt).IsRequired();
        builder.Property(p => p.Password).IsRequired();

        builder.HasOne(e => e.ArchivedUser)
            .WithOne(a => a.Employee)
            .HasForeignKey<ArchivedUserEntity>(a => a.EmployeeId);

        builder.HasQueryFilter(e => !e.IsArchived);
    }
}