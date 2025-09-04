namespace Reshebnik.Domain.Entities;

public class SystemNotificationEntity
{
    public int Id { get; set; }
    public string Caption { get; set; } = null!;
    public string Message { get; set; } = null!;

    public NotificationType Type { get; set; }
    public DateTime CreaetedAt { get; set; }

    public int? CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;
}

public class UserNotification
{
    public int EmployeeId { get; set; }
    public EmployeeEntity User { get; set; } = null!;

    public int NotificationId { get; set; }
    public SystemNotificationEntity Notification { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}

public enum NotificationType
{
    Normal = 0,
    Warning = 1,
    Critical = 2,
}