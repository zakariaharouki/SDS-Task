using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SchemeTracker.Api.Validation;

/// <summary>
/// Builds the 400 response for requests that fail before reaching a controller action. It replaces
/// framework messages with user-facing ones, and uses camelCase field paths the frontend can map to inputs.
/// </summary>
public static partial class InvalidRequestResponse
{
    private const string BodyField = "body";

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["name"] = "Name",
        ["siteAddress"] = "Site address",
        ["status"] = "Status",
        ["tenureMix"] = "Tenure mix",
        ["socialRent"] = "Social rent",
        ["affordableRent"] = "Affordable rent",
        ["sharedOwnership"] = "Shared ownership",
        ["marketSale"] = "Market sale",
        ["homes"] = "Homes",
        ["averageValue"] = "Average value",
        ["version"] = "Version"
    };

    public static IActionResult Create(ActionContext context)
    {
        var errors = new ModelStateDictionary();

        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors)
            {
                if (!IsActionParameterRequiredError(context, key, error.ErrorMessage))
                {
                    AddFriendlyError(errors, key, error.ErrorMessage);
                }
            }
        }

        var problem = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>()
            .CreateValidationProblemDetails(context.HttpContext, errors);

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    private static void AddFriendlyError(ModelStateDictionary errors, string key, string message)
    {
        if (key.Length == 0)
        {
            errors.AddModelError(BodyField, "A request body is required.");
        }
        else if (!key.StartsWith('$'))
        {
            AddBindingError(errors, key, message);
        }
        else
        {
            AddJsonError(errors, ToFieldPath(key), message);
        }
    }

    // Errors from model binding: null values for required fields, and query string values.
    private static void AddBindingError(ModelStateDictionary errors, string key, string message)
    {
        var path = ToFieldPath(key);

        errors.AddModelError(path, message.EndsWith("field is required.")
            ? $"{Label(LastSegment(path))} is required."
            : message);
    }

    private static void AddJsonError(ModelStateDictionary errors, string path, string message)
    {
        var missing = MissingPropertiesPattern().Match(message);
        var unknown = UnknownPropertyPattern().Match(message);

        if (missing.Success)
        {
            foreach (var name in missing.Groups["names"].Value.Split(", "))
            {
                errors.AddModelError(Combine(path, name), $"{Label(name)} is required.");
            }
        }
        else if (unknown.Success)
        {
            var name = unknown.Groups["name"].Value;
            var fieldPath = LastSegment(path) == name ? path : Combine(path, name);

            errors.AddModelError(fieldPath, "This field is not allowed.");
        }
        else if (message.StartsWith("The JSON value could not be converted") && path.Length > 0)
        {
            errors.AddModelError(path, WrongTypeMessage(LastSegment(path)));
        }
        else
        {
            errors.AddModelError(BodyField, "The request body is not valid JSON.");
        }
    }

    private static string WrongTypeMessage(string field) => field switch
    {
        "homes" or "version" => $"{Label(field)} must be a whole number.",
        "averageValue" => $"{Label(field)} must be a number.",
        "status" => "Status must be one of: Appraisal, Planning, OnSite, Complete.",
        "name" or "siteAddress" => $"{Label(field)} must be text.",
        "tenureMix" or "socialRent" or "affordableRent" or "sharedOwnership" or "marketSale" =>
            $"{Label(field)} must be an object.",
        _ => "This value is not valid."
    };

    // When a body fails to parse, the framework also reports the whole action parameter as required.
    private static bool IsActionParameterRequiredError(ActionContext context, string key, string message) =>
        message.EndsWith("field is required.")
        && context.ActionDescriptor.Parameters.Any(parameter => parameter.Name == key);

    private static string ToFieldPath(string key)
    {
        var segments = key.TrimStart('$').TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries);

        return string.Join('.', segments.Select(segment => char.ToLowerInvariant(segment[0]) + segment[1..]));
    }

    private static string Combine(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";

    private static string LastSegment(string path) => path[(path.LastIndexOf('.') + 1)..];

    private static string Label(string field) => Labels.GetValueOrDefault(field, field);

    [GeneratedRegex("missing required properties,? including(?: the following)?: (?<names>.+)$")]
    private static partial Regex MissingPropertiesPattern();

    [GeneratedRegex("The JSON property '(?<name>.+?)' could not be mapped")]
    private static partial Regex UnknownPropertyPattern();
}
