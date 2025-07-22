using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class BugHuntEntityConfiguration : IEntityTypeConfiguration<BugHuntEntity>
{
    public void Configure(EntityTypeBuilder<BugHuntEntity> builder)
    {
        builder.ToTable("bug_hunts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.Message)
            .HasColumnName("message")
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(b => b.Screenshot)
            .HasColumnName("screenshot");

        builder.Property(b => b.LastRequestUrl)
            .HasColumnName("last_request_url")
            .HasMaxLength(1024);

        builder.Property(b => b.LastRequestResponse)
            .HasColumnName("last_request_response");

        builder.Property(b => b.LastRequestStatus)
            .HasColumnName("last_request_status");

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
