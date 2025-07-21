using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Reshebnik.Domain.Entities;
using Reshebnik.EntityFramework.Utils;

namespace Reshebnik.EntityFramework;

public class ReshebnikContext(DbContextOptions<ReshebnikContext> options) : DbContext(options)
{
    public DbSet<CompanyEntity> Companies { get; set; }

    public DbSet<EmployeeEntity> Employees { get; set; }

    public DbSet<DepartmentEntity> Departments { get; set; }
    public DbSet<DepartmentSchemeEntity> DepartmentSchemaEntities { get; set; }
    public DbSet<EmployeeDepartmentLinkEntity> EmployeeDepartmentLinkEntities { get; set; }

    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<SystemNotificationEntity> SystemNotifications { get; set; }
    public DbSet<MetricEntity> Metrics { get; set; }
    public DbSet<MetricTemplateEntity> MetricTemplates { get; set; }
    public DbSet<IndicatorEntity> Indicators { get; set; }

    public DbSet<EmailMessageEntity> EmailQueue { get; set; }

    public DbSet<SpecialInvitationEntity> SpecialInvitations { get; set; }

    public DbSet<LogExceptionEntity> ExceptionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("employee_id_seq");
        modelBuilder.HasSequence<int>("companies_id_seq");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new UtcValueConverter());
                }
            }
        }

        // UTC dates only:
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(dateTimeConverter);

                if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableDateTimeConverter);
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReshebnikContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}