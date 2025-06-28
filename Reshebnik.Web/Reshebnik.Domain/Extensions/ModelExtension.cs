namespace Reshebnik.Domain.Extensions;

public static class ModelExtension
{
    public static void EnsurePropertyExists<T, U>(this T model, Func<T, U> property)
    {
        if (property(model)!.Equals(default))
            throw new ArgumentNullException(nameof(property));
    }
}