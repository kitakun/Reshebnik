using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Reshebnik.Web;

public static class TimeZoneHelper
{
    public static IHttpContextAccessor? HttpContextAccessor { get; set; }

    public static TimeSpan CurrentOffset
    {
        get
        {
            var accessor = HttpContextAccessor;
            if (accessor?.HttpContext == null) return TimeSpan.Zero;
            var header = accessor.HttpContext.Request.Headers["tzone"].FirstOrDefault();
            if (header != null && TimeSpan.TryParse(header, out var offset))
            {
                return offset;
            }
            return TimeSpan.Zero;
        }
    }

    public static DateTime ConvertToUserTime(DateTime utcTime)
    {
        utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        return utcTime + CurrentOffset;
    }

    public static DateTime ConvertToUtc(DateTime localTime)
    {
        localTime = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        return localTime - CurrentOffset;
    }
}
