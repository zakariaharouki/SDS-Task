using Microsoft.AspNetCore.Mvc.ModelBinding;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Validation;

/// <summary>
/// Accepts only the exact status name in the query string. The built-in enum binder also accepts
/// numbers and ignores case.
/// </summary>
public sealed class SchemeStatusModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (values == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, values);

        if (SchemeStatusNames.TryParseExact(values.FirstValue, out var status))
        {
            bindingContext.Result = ModelBindingResult.Success(status);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, InvalidRequestResponse.InvalidStatusMessage);
        }

        return Task.CompletedTask;
    }
}

public sealed class SchemeStatusModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType == typeof(SchemeStatus) ? new SchemeStatusModelBinder() : null;
}
