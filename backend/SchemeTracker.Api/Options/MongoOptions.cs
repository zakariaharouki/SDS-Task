using System.ComponentModel.DataAnnotations;

namespace SchemeTracker.Api.Options;

public class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string Database { get; set; } = string.Empty;
}
