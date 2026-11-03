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

public static class SchemeStatusNames
{
    /// <summary>The order schemes move through. Used for sorting and for the summary rows.</summary>
    public static readonly IReadOnlyList<SchemeStatus> PipelineOrder =
    [
        SchemeStatus.Appraisal,
        SchemeStatus.Planning,
        SchemeStatus.OnSite,
        SchemeStatus.Complete
    ];

    /// <summary>Matches only the exact status name, unlike Enum.Parse, which also accepts numbers.</summary>
    public static bool TryParseExact(string? value, out SchemeStatus result)
    {
        foreach (var status in Enum.GetValues<SchemeStatus>())
        {
            if (status.ToString() == value)
            {
                result = status;
                return true;
            }
        }

        result = default;
        return false;
    }
}
