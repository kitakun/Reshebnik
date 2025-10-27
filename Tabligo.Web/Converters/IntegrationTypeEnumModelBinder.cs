using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tabligo.Domain.Enums;

namespace Tabligo.Web.Converters;

public class IntegrationTypeEnumModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var stringValue = value.FirstValue;
        if (string.IsNullOrEmpty(stringValue))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        // Convert string to enum (case-insensitive)
        if (Enum.TryParse<IntegrationTypeEnum>(stringValue, ignoreCase: true, out var enumValue))
        {
            bindingContext.Result = ModelBindingResult.Success(enumValue);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"Cannot convert '{stringValue}' to {nameof(IntegrationTypeEnum)}");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}

public class IntegrationTypeEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(IntegrationTypeEnum))
        {
            return new IntegrationTypeEnumModelBinder();
        }

        return null;
    }
}
