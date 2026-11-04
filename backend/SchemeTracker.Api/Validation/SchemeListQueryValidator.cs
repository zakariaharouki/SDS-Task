using FluentValidation;
using SchemeTracker.Api.Dtos;

namespace SchemeTracker.Api.Validation;

/// <summary>The sort rules from design section 4.1. Status values are checked when they are bound.</summary>
public class SchemeListQueryValidator : AbstractValidator<SchemeListQuery>
{
    private static readonly string[] SortFields = ["status", "name", "totalHomes", "totalValue", "updatedAt"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public SchemeListQueryValidator()
    {
        RuleFor(query => query.SortBy)
            .Must(sortBy => SortFields.Contains(sortBy))
            .WithMessage($"Sort by must be one of: {string.Join(", ", SortFields)}.");

        RuleFor(query => query.SortDir)
            .Must(sortDir => SortDirections.Contains(sortDir))
            .WithMessage("Sort direction must be asc or desc.");
    }
}
