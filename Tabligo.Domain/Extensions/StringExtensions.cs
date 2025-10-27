namespace Tabligo.Domain.Extensions;

public static class StringExtensions
{
    public static string Mask(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        return value.Length > 8 
            ? $"{value.Substring(0, 4)}***{value.Substring(value.Length - 4)}" 
            : "[SECRET]";
    }
}

