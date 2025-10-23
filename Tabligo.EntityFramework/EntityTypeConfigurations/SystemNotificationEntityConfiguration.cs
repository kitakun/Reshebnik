using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Tabligo.Domain.Entities;

namespace Tabligo.EntityFramework.EntityTypeConfigurations;

public class SystemNotificationEntityConfiguration : IEntityTypeConfiguration<SystemNotificationEntity>
{
    public void Configure(EntityTypeBuilder<SystemNotificationEntity> builder)
    {
        builder.ToTable("system_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Caption)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(200_000);

        builder.Property(n => n.Type)
            .IsRequired();

        builder.Property(n => n.CreaetedAt)
            .IsRequired();

        builder.HasOne(n => n.Company)
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}