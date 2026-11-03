using System.Text.Json.Serialization;

namespace SchemeTracker.Api.Models;

[JsonConverter(typeof(SchemeStatusJsonConverter))]
public enum SchemeStatus
{
    Appraisal,
    Planning,
    OnSite,
    Complete
}
