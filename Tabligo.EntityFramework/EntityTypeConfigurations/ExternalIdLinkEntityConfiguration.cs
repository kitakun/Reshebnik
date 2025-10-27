using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class ExternalIdLinkEntityConfiguration : IEntityTypeConfiguration<ExternalIdLinkEntity>
{
    public void Configure(EntityTypeBuilder<ExternalIdLinkEntity> builder)
    {
        builder.ToTable("external_id_links");
        builder.HasKey(x => x.Id);
        
        // Basic properties
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ExternalId).HasColumnName("external_id").IsRequired().HasMaxLength(500);
        builder.Property(x => x.IntegrationType).HasColumnName("integration_type").IsRequired();
        
        // Polymorphic fields for indexing
        builder.Property(x => x.EntityType).HasColumnName("entity_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        
        // Direct navigation properties
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.MetricId).HasColumnName("metric_id");
        builder.Property(x => x.IndicatorId).HasColumnName("indicator_id");
        
        // Indexes
        builder.HasIndex(x => new { x.CompanyId, x.ExternalId, x.IntegrationType, x.EntityType })
               .HasDatabaseName("ix_external_id_links_lookup");
        builder.HasIndex(x => new { x.CompanyId, x.EntityType, x.EntityId })
               .HasDatabaseName("ix_external_id_links_entity")
               .IsUnique();
        
        // Relationships
        builder.HasOne(x => x.Company)
               .WithMany(c => c.ExternalIdLinks)
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Employee)
               .WithMany(e => e.ExternalIdLinks)
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Department)
               .WithMany(d => d.ExternalIdLinks)
               .HasForeignKey(x => x.DepartmentId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Metric)
               .WithMany(m => m.ExternalIdLinks)
               .HasForeignKey(x => x.MetricId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Indicator)
               .WithMany(i => i.ExternalIdLinks)
               .HasForeignKey(x => x.IndicatorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
