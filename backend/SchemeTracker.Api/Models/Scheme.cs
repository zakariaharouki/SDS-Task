using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SchemeTracker.Api.Models;

public class Scheme
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("siteAddress")]
    public string SiteAddress { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SchemeStatus Status { get; set; }

    [BsonElement("tenureMix")]
    public TenureMix TenureMix { get; set; } = new();

    [BsonElement("totals")]
    public SchemeTotals Totals { get; set; } = new();

    [BsonElement("duplicateKey")]
    public string DuplicateKey { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}
