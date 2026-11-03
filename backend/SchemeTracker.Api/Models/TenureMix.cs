using MongoDB.Bson.Serialization.Attributes;

namespace SchemeTracker.Api.Models;

public class TenureMix
{
    [BsonElement("socialRent")]
    public TenureLine SocialRent { get; set; } = new();

    [BsonElement("affordableRent")]
    public TenureLine AffordableRent { get; set; } = new();

    [BsonElement("sharedOwnership")]
    public TenureLine SharedOwnership { get; set; } = new();

    [BsonElement("marketSale")]
    public TenureLine MarketSale { get; set; } = new();
}
