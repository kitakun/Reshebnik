namespace Tabligo.Domain.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtcFromClient(this DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc) return value;
        // Assume incoming value is in local time when kind is Unspecified or Local
        var local = DateTime.SpecifyKind(value, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    public static DateTime? ToUtcFromClient(this DateTime? value)
    {
        return value.HasValue ? value.Value.ToUtcFromClient() : null;
    }
}
