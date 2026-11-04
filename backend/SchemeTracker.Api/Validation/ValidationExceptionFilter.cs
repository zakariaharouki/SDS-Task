using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SchemeTracker.Api.Validation;

/// <summary>Turns a validation failure from a service into the standard 400 response.</summary>
public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException exception)
        {
            return;
        }

        var errors = new ModelStateDictionary();

        foreach (var failure in exception.Errors)
        {
            errors.AddModelError(InvalidRequestResponse.ToFieldPath(failure.PropertyName), failure.ErrorMessage);
        }

        context.Result = InvalidRequestResponse.FromErrors(context, errors);
        context.ExceptionHandled = true;
    }
}
