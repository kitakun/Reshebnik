﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reshebnik.Domain.Entities;

namespace Reshebnik.EntityFramework.EntityTypeConfigurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("user_notifications");

        builder.HasKey(un => new { un.EmployeeId, un.NotificationId });

        builder.Property(un => un.IsRead)
            .IsRequired();

        builder.Property(un => un.ReadAt)
            .IsRequired(false);

        builder.HasOne(un => un.User)
            .WithMany()
            .HasForeignKey(un => un.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(un => un.Notification)
            .WithMany()
            .HasForeignKey(un => un.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.User).WithMany(m => m.UserNotification).HasForeignKey(k => k.EmployeeId);
    }
}