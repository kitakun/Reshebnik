using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class EmailMessageEntityConfiguration : IEntityTypeConfiguration<EmailMessageEntity>
{
    public void Configure(EntityTypeBuilder<EmailMessageEntity> builder)
    {
        builder.ToTable("email_messages");

        builder.HasKey(k => k.Id);

        builder.Property(e => e.To).HasColumnName("to").IsRequired();
        builder.Property(e => e.Subject).HasColumnName("subject").IsRequired();
        builder.Property(e => e.Body).HasColumnName("body").IsRequired();
        builder.Property(e => e.Error).HasColumnName("error");
        builder.Property(e => e.IsHtml).HasColumnName("is_html").HasDefaultValue(true);
        builder.Property(e => e.From).HasColumnName("from").HasDefaultValue("no-reply@mydoc.com");

        builder.Property(e => e.EnqueuedAt).HasColumnName("enqueued_at").IsRequired();
        builder.Property(e => e.IsSent).HasColumnName("is_sent").IsRequired();
        builder.Property(e => e.SentAt).HasColumnName("sent_at");

        builder.HasOne(e => e.SentByUser)
            .WithMany()
            .HasForeignKey(e => e.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Cc)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property(e => e.Bcc)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.OwnsMany(e => e.Attachments, nav =>
        {
            nav.ToTable("EmailAttachments");

            nav.WithOwner().HasForeignKey("EmailMessageId");

            nav.HasKey("EmailMessageId", "FileName");

            nav.Property(a => a.FileName).IsRequired();
            nav.Property(a => a.Content).IsRequired();
            nav.Property(a => a.ContentType).HasDefaultValue("application/octet-stream");
            
            builder.HasOne(e => e.SentByCompany)
                .WithMany()
                .HasForeignKey(e => e.SentByCompanyId);
        });
    }
}