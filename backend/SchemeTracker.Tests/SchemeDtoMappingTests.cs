using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Mapping;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Tests;

public class SchemeDtoMappingTests
{
    private static TenureMixDto BuildMix() => new()
    {
        SocialRent = new TenureLineDto { Homes = 1, AverageValue = 100.10m },
        AffordableRent = new TenureLineDto { Homes = 2, AverageValue = 200.20m },
        SharedOwnership = new TenureLineDto { Homes = 3, AverageValue = 300.30m },
        MarketSale = new TenureLineDto { Homes = 4, AverageValue = 400.40m }
    };

    [Fact]
    public void Create_request_maps_each_tenure_to_its_own_line()
    {
        var scheme = new CreateSchemeRequest
        {
            Name = "Riverside",
            SiteAddress = "Leeds",
            Status = SchemeStatus.Planning,
            TenureMix = BuildMix()
        }.ToModel();

        Assert.Equal("Riverside", scheme.Name);
        Assert.Equal("Leeds", scheme.SiteAddress);
        Assert.Equal(SchemeStatus.Planning, scheme.Status);
        Assert.Equal(100.10m, scheme.TenureMix.SocialRent.AverageValue);
        Assert.Equal(200.20m, scheme.TenureMix.AffordableRent.AverageValue);
        Assert.Equal(300.30m, scheme.TenureMix.SharedOwnership.AverageValue);
        Assert.Equal(400.40m, scheme.TenureMix.MarketSale.AverageValue);
        Assert.Equal(4, scheme.TenureMix.MarketSale.Homes);
    }

    [Fact]
    public void Update_request_leaves_server_set_fields_alone()
    {
        var createdAt = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var scheme = new Scheme { Id = "6725f0a1b2c3d4e5f6a7b8c9", Version = 3, CreatedAt = createdAt };

        new UpdateSchemeRequest
        {
            Name = "Renamed",
            SiteAddress = "York",
            Status = SchemeStatus.OnSite,
            TenureMix = BuildMix(),
            Version = 99
        }.ApplyTo(scheme);

        Assert.Equal("Renamed", scheme.Name);
        Assert.Equal(SchemeStatus.OnSite, scheme.Status);
        Assert.Equal("6725f0a1b2c3d4e5f6a7b8c9", scheme.Id);
        Assert.Equal(3, scheme.Version);
        Assert.Equal(createdAt, scheme.CreatedAt);
    }

    [Fact]
    public void Response_takes_figures_from_stored_totals_and_the_given_percent()
    {
        var scheme = new Scheme
        {
            Totals = new SchemeTotals { TotalHomes = 10, AffordableHomes = 6, TotalValue = 123456.78m }
        };

        var response = scheme.ToResponse(affordablePercent: null);

        Assert.Equal(10, response.TotalHomes);
        Assert.Equal(6, response.AffordableHomes);
        Assert.Equal(123456.78m, response.TotalValue);
        Assert.Null(response.AffordablePercent);
    }
}
