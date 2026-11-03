using System.Text.Json.Serialization;

namespace SchemeTracker.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SchemeStatus>))]
public enum SchemeStatus
{
    Appraisal,
    Planning,
    OnSite,
    Complete
}
