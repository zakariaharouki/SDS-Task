using FluentValidation;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Validation;

/// <summary>The field rules from design section 3, shared by create and update requests.</summary>
public class SchemeRequestValidator : AbstractValidator<ISchemeRequest>
{
    private const int MaxNameLength = 200;
    private const int MaxAddressLength = 300;

    public SchemeRequestValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name is required.")
            .Must(name => name.Trim().Length <= MaxNameLength).WithMessage($"Name must be {MaxNameLength} characters or fewer.");

        RuleFor(request => request.SiteAddress)
            .Cascade(CascadeMode.Stop)
            .Must(address => !string.IsNullOrWhiteSpace(address)).WithMessage("Site address is required.")
            .Must(address => address.Trim().Length <= MaxAddressLength).WithMessage($"Site address must be {MaxAddressLength} characters or fewer.");

        RuleFor(request => request.Status)
            .IsInEnum().WithMessage("Status must be one of: Appraisal, Planning, OnSite, Complete.");

        var lineValidator = new TenureLineValidator();
        RuleFor(request => request.TenureMix.SocialRent).SetValidator(lineValidator);
        RuleFor(request => request.TenureMix.AffordableRent).SetValidator(lineValidator);
        RuleFor(request => request.TenureMix.SharedOwnership).SetValidator(lineValidator);
        RuleFor(request => request.TenureMix.MarketSale).SetValidator(lineValidator);

        RuleFor(request => request.TenureMix)
            .Must(HaveAtLeastOneHome)
            .When(request => request.Status != SchemeStatus.Appraisal)
            .WithMessage("A scheme in Planning, On site or Complete must have at least 1 home.");
    }

    private static bool HaveAtLeastOneHome(TenureMixDto mix) =>
        mix.SocialRent.Homes > 0
        || mix.AffordableRent.Homes > 0
        || mix.SharedOwnership.Homes > 0
        || mix.MarketSale.Homes > 0;
}

public class TenureLineValidator : AbstractValidator<TenureLineDto>
{
    private const int MaxHomes = 10_000;
    private const decimal MaxAverageValue = 10_000_000m;

    public TenureLineValidator()
    {
        RuleFor(line => line.Homes)
            .InclusiveBetween(0, MaxHomes).WithMessage("Homes must be between 0 and 10,000.");

        RuleFor(line => line.AverageValue)
            .Cascade(CascadeMode.Stop)
            .InclusiveBetween(0m, MaxAverageValue).WithMessage("Average value must be between £0 and £10,000,000.")
            .Must(HaveAtMostTwoDecimalPlaces).WithMessage("Average value can have at most 2 decimal places.")
            .Must((line, averageValue) => line.Homes <= 0 || averageValue > 0)
            .WithMessage("Average value must be greater than £0 when there are homes.");
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal value) => decimal.Round(value, 2) == value;
}
