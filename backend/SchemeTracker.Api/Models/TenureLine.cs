using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SchemeTracker.Api.Models;

public class TenureLine
{
    [BsonElement("homes")]
    public int Homes { get; set; }

    [BsonElement("averageValue")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageValue { get; set; }
}
