using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class SpecialInvitationEntityConfiguration : IEntityTypeConfiguration<SpecialInvitationEntity>
{
    public void Configure(EntityTypeBuilder<SpecialInvitationEntity> builder)
    {
        builder.ToTable("special_invitations");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.FIO).HasColumnName("fio").IsRequired().HasMaxLength(256);
        builder.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
        builder.Property(e => e.CompanyName).HasColumnName("company_name").IsRequired().HasMaxLength(256);
        builder.Property(e => e.CompanyDescription).HasColumnName("company_description").IsRequired().HasMaxLength(1024);
        builder.Property(e => e.CompanySize).HasColumnName("company_size").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.Granted).HasColumnName("granted").IsRequired().HasDefaultValue(false);
    }
}
