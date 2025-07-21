using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class LogExceptionEntityConfiguration : IEntityTypeConfiguration<LogExceptionEntity>
{
    public void Configure(EntityTypeBuilder<LogExceptionEntity> builder)
    {
        builder.ToTable("log_exceptions");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(l => l.StackTrace)
            .HasColumnName("stacktrace")
            .IsRequired();

        builder.Property(l => l.UserEmail)
            .HasColumnName("user_email")
            .HasMaxLength(256);

        builder.Property(l => l.CompanyId)
            .HasColumnName("company_id");

        builder.HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
