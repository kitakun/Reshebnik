using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tabligo.EntityFramework.Utils;

public class UtcValueConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc
        ? v
        : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));