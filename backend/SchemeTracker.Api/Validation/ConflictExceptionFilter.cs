using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SchemeTracker.Api.Exceptions;

namespace SchemeTracker.Api.Validation;

/// <summary>
/// Turns conflicts with existing data into 409 responses. The two kinds have different titles
/// so the frontend can tell them apart (design section 4.4).
/// </summary>
public sealed class ConflictExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var problems = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        ProblemDetails? problem = context.Exception switch
        {
            DuplicateSchemeException exception => DuplicateProblem(context, problems, exception),
            SchemeVersionConflictException exception => problems.CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status409Conflict,
                title: "This scheme was changed by someone else.",
                detail: exception.Message),
            _ => null
        };

        if (problem is null)
        {
            return;
        }

        context.Result = new ConflictObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }

    private static ValidationProblemDetails DuplicateProblem(
        ExceptionContext context,
        ProblemDetailsFactory problems,
        DuplicateSchemeException exception)
    {
        var errors = new ModelStateDictionary();
        errors.AddModelError("name", exception.Message);
        errors.AddModelError("siteAddress", exception.Message);

        return problems.CreateValidationProblemDetails(
            context.HttpContext,
            errors,
            StatusCodes.Status409Conflict,
            title: "This scheme already exists.");
    }
}
