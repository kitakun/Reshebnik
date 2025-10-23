using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class JobOperationEntityConfiguration : IEntityTypeConfiguration<JobOperationEntity>
{
    public void Configure(EntityTypeBuilder<JobOperationEntity> builder)
    {
        builder.ToTable("job_operations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(e => e.Hash)
            .HasColumnName("hash")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(e => e.InputData)
            .HasColumnName("input_data")
            .HasColumnType("jsonb");

        builder.Property(e => e.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb");

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Hash);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}
