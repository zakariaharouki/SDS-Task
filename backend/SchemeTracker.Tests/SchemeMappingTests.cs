using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Tests;

public class SchemeMappingTests
{
    private static Scheme BuildScheme() => new()
    {
        Id = "6725f0a1b2c3d4e5f6a7b8c9",
        Name = "Riverside Phase 2",
        SiteAddress = "Unit 4, Riverside Road, Leeds LS1 4AB",
        Status = SchemeStatus.OnSite,
        TenureMix = new TenureMix
        {
            SocialRent = new TenureLine { Homes = 40, AverageValue = 150000.10m },
            AffordableRent = new TenureLine { Homes = 30, AverageValue = 180000.20m },
            SharedOwnership = new TenureLine { Homes = 20, AverageValue = 220000.30m },
            MarketSale = new TenureLine { Homes = 60, AverageValue = 123456.78m }
        },
        Totals = new SchemeTotals { TotalHomes = 150, AffordableHomes = 90, TotalValue = 37400000.55m },
        DuplicateKey = "riverside phase 2|unit 4, riverside road, leeds ls1 4ab",
        Version = 3,
        CreatedAt = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 11, 3, 9, 12, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Scheme_uses_the_stored_field_names_from_the_design()
    {
        var document = BuildScheme().ToBsonDocument();

        Assert.Equal(
            ["_id", "name", "siteAddress", "status", "tenureMix", "totals", "duplicateKey", "version", "createdAt", "updatedAt"],
            document.Names);
        Assert.Equal(
            ["socialRent", "affordableRent", "sharedOwnership", "marketSale"],
            document["tenureMix"].AsBsonDocument.Names);
        Assert.Equal(
            ["homes", "averageValue"],
            document["tenureMix"]["marketSale"].AsBsonDocument.Names);
        Assert.Equal(
            ["totalHomes", "affordableHomes", "totalValue"],
            document["totals"].AsBsonDocument.Names);
    }

    [Fact]
    public void Id_is_stored_as_ObjectId()
    {
        var document = BuildScheme().ToBsonDocument();

        Assert.Equal(BsonType.ObjectId, document["_id"].BsonType);
    }

    [Fact]
    public void Money_is_stored_as_Decimal128()
    {
        var document = BuildScheme().ToBsonDocument();
        var mix = document["tenureMix"];

        Assert.Equal(BsonType.Decimal128, mix["socialRent"]["averageValue"].BsonType);
        Assert.Equal(BsonType.Decimal128, mix["affordableRent"]["averageValue"].BsonType);
        Assert.Equal(BsonType.Decimal128, mix["sharedOwnership"]["averageValue"].BsonType);
        Assert.Equal(BsonType.Decimal128, mix["marketSale"]["averageValue"].BsonType);
        Assert.Equal(BsonType.Decimal128, document["totals"]["totalValue"].BsonType);
    }

    [Fact]
    public void Status_is_stored_as_its_name()
    {
        var document = BuildScheme().ToBsonDocument();

        Assert.Equal(BsonType.String, document["status"].BsonType);
        Assert.Equal("OnSite", document["status"].AsString);
    }

    [Fact]
    public void Status_is_serialized_to_json_as_its_name()
    {
        Assert.Equal("\"OnSite\"", JsonSerializer.Serialize(SchemeStatus.OnSite));
    }

    [Fact]
    public void Scheme_round_trips_without_changing_values()
    {
        var original = BuildScheme();

        var restored = BsonSerializer.Deserialize<Scheme>(original.ToBsonDocument());

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(123456.78m, restored.TenureMix.MarketSale.AverageValue);
        Assert.Equal(37400000.55m, restored.Totals.TotalValue);
        Assert.Equal(original.CreatedAt, restored.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, restored.CreatedAt.Kind);
    }
}
