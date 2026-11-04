using FluentValidation.TestHelper;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Validation;

namespace SchemeTracker.Tests;

public class SchemeRequestValidatorTests
{
    private const string AtLeastOneHome = "A scheme in Planning, On site or Complete must have at least 1 home.";

    private readonly SchemeRequestValidator _validator = new();

    private static TenureLineDto Line(int homes = 10, decimal averageValue = 100m) =>
        new() { Homes = homes, AverageValue = averageValue };

    private static TenureMixDto Mix(TenureLineDto line) => new()
    {
        SocialRent = line,
        AffordableRent = line,
        SharedOwnership = line,
        MarketSale = line
    };

    private static CreateSchemeRequest Request(
        string name = "Riverside",
        string siteAddress = "Leeds",
        SchemeStatus status = SchemeStatus.Planning,
        TenureMixDto? mix = null) => new()
    {
        Name = name,
        SiteAddress = siteAddress,
        Status = status,
        TenureMix = mix ?? Mix(Line())
    };

    private TestValidationResult<ISchemeRequest> Validate(ISchemeRequest request) =>
        _validator.TestValidate(request);

    [Fact]
    public void Valid_request_has_no_errors() =>
        Validate(Request()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Update_requests_follow_the_same_rules()
    {
        var request = new UpdateSchemeRequest
        {
            Name = " ",
            SiteAddress = "Leeds",
            Status = SchemeStatus.Planning,
            TenureMix = Mix(Line()),
            Version = 1
        };

        Validate(request).ShouldHaveValidationErrorFor(nameof(request.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Blank_name_is_required(string name) =>
        Validate(Request(name: name))
            .ShouldHaveValidationErrorFor("Name")
            .WithErrorMessage("Name is required.");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_address_is_required(string address) =>
        Validate(Request(siteAddress: address))
            .ShouldHaveValidationErrorFor("SiteAddress")
            .WithErrorMessage("Site address is required.");

    [Fact]
    public void Name_of_exactly_200_characters_is_valid() =>
        Validate(Request(name: new string('x', 200))).ShouldNotHaveValidationErrorFor("Name");

    [Fact]
    public void Name_of_201_characters_is_rejected() =>
        Validate(Request(name: new string('x', 201)))
            .ShouldHaveValidationErrorFor("Name")
            .WithErrorMessage("Name must be 200 characters or fewer.");

    [Fact]
    public void Name_length_is_measured_after_trimming() =>
        Validate(Request(name: "   " + new string('x', 200) + "   ")).ShouldNotHaveValidationErrorFor("Name");

    [Fact]
    public void Address_of_exactly_300_characters_is_valid() =>
        Validate(Request(siteAddress: new string('x', 300))).ShouldNotHaveValidationErrorFor("SiteAddress");

    [Fact]
    public void Address_of_301_characters_is_rejected() =>
        Validate(Request(siteAddress: new string('x', 301)))
            .ShouldHaveValidationErrorFor("SiteAddress")
            .WithErrorMessage("Site address must be 300 characters or fewer.");

    [Fact]
    public void Address_length_is_measured_after_trimming() =>
        Validate(Request(siteAddress: "  " + new string('x', 300) + "  ")).ShouldNotHaveValidationErrorFor("SiteAddress");

    [Fact]
    public void Undefined_status_is_rejected() =>
        Validate(Request(status: (SchemeStatus)99)).ShouldHaveValidationErrorFor("Status");

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    [InlineData(int.MaxValue)]
    public void Homes_out_of_range_are_rejected(int homes) =>
        Validate(Request(mix: Mix(Line(homes: homes))))
            .ShouldHaveValidationErrorFor("TenureMix.MarketSale.Homes")
            .WithErrorMessage("Homes must be between 0 and 10,000.");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10_000)]
    public void Homes_at_the_limits_are_valid(int homes) =>
        Validate(Request(status: SchemeStatus.Appraisal, mix: Mix(Line(homes: homes))))
            .ShouldNotHaveValidationErrorFor("TenureMix.MarketSale.Homes");

    [Fact]
    public void Each_tenure_is_checked()
    {
        var bad = Line(homes: -1);
        var result = Validate(Request(mix: Mix(bad)));

        result.ShouldHaveValidationErrorFor("TenureMix.SocialRent.Homes");
        result.ShouldHaveValidationErrorFor("TenureMix.AffordableRent.Homes");
        result.ShouldHaveValidationErrorFor("TenureMix.SharedOwnership.Homes");
        result.ShouldHaveValidationErrorFor("TenureMix.MarketSale.Homes");
    }

    [Fact]
    public void Zero_average_value_with_homes_is_rejected() =>
        Validate(Request(mix: Mix(Line(homes: 1, averageValue: 0m))))
            .ShouldHaveValidationErrorFor("TenureMix.MarketSale.AverageValue")
            .WithErrorMessage("Average value must be greater than £0 when there are homes.");

    [Theory]
    [InlineData("0")]
    [InlineData("250000.50")]
    public void Any_valid_average_value_is_allowed_with_zero_homes(string averageValue) =>
        Validate(Request(status: SchemeStatus.Appraisal, mix: Mix(Line(homes: 0, averageValue: decimal.Parse(averageValue)))))
            .ShouldNotHaveValidationErrorFor("TenureMix.MarketSale.AverageValue");

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10000000.01")]
    public void Average_value_out_of_range_is_rejected(string averageValue) =>
        Validate(Request(mix: Mix(Line(averageValue: decimal.Parse(averageValue)))))
            .ShouldHaveValidationErrorFor("TenureMix.MarketSale.AverageValue")
            .WithErrorMessage("Average value must be between £0 and £10,000,000.");

    [Theory]
    [InlineData("0.01")]
    [InlineData("123456.78")]
    [InlineData("100.100")]
    [InlineData("10000000")]
    public void Average_value_within_the_rules_is_valid(string averageValue) =>
        Validate(Request(mix: Mix(Line(averageValue: decimal.Parse(averageValue)))))
            .ShouldNotHaveValidationErrorFor("TenureMix.MarketSale.AverageValue");

    [Theory]
    [InlineData("100.123")]
    [InlineData("0.001")]
    public void Average_value_with_three_decimal_places_is_rejected(string averageValue) =>
        Validate(Request(mix: Mix(Line(averageValue: decimal.Parse(averageValue)))))
            .ShouldHaveValidationErrorFor("TenureMix.MarketSale.AverageValue")
            .WithErrorMessage("Average value can have at most 2 decimal places.");

    [Fact]
    public void Zero_total_homes_is_valid_at_appraisal() =>
        Validate(Request(status: SchemeStatus.Appraisal, mix: Mix(Line(homes: 0, averageValue: 0m))))
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(SchemeStatus.Planning)]
    [InlineData(SchemeStatus.OnSite)]
    [InlineData(SchemeStatus.Complete)]
    public void Zero_total_homes_is_rejected_past_appraisal(SchemeStatus status) =>
        Validate(Request(status: status, mix: Mix(Line(homes: 0, averageValue: 0m))))
            .ShouldHaveValidationErrorFor("TenureMix")
            .WithErrorMessage(AtLeastOneHome);

    [Fact]
    public void One_home_at_planning_is_valid()
    {
        var mix = new TenureMixDto
        {
            SocialRent = Line(homes: 0, averageValue: 0m),
            AffordableRent = Line(homes: 0, averageValue: 0m),
            SharedOwnership = Line(homes: 0, averageValue: 0m),
            MarketSale = Line(homes: 1, averageValue: 1m)
        };

        Validate(Request(status: SchemeStatus.Planning, mix: mix)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Maximum_values_are_valid() =>
        Validate(Request(mix: Mix(Line(homes: 10_000, averageValue: 10_000_000m)))).ShouldNotHaveAnyValidationErrors();
}
