using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class CategoryRecordEntityConfiguration : IEntityTypeConfiguration<CategoryRecordEntity>
{
    public void Configure(EntityTypeBuilder<CategoryRecordEntity> builder)
    {
        builder.ToTable("category_records");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1024);

        builder.HasIndex(c => new { c.CompanyId, c.Name }).IsUnique();
    }
}
