using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SchemeTracker.Api.Models;

public class SchemeTotals
{
    [BsonElement("totalHomes")]
    public int TotalHomes { get; set; }

    [BsonElement("affordableHomes")]
    public int AffordableHomes { get; set; }

    [BsonElement("totalValue")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalValue { get; set; }
}
