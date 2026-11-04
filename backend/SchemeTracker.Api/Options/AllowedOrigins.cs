namespace SchemeTracker.Api.Options;

/// <summary>Reads the browser origins allowed to call the API (design section 5.3).</summary>
public static class AllowedOrigins
{
    public const string ConfigurationKey = "Cors:AllowedOrigins";

    private const string DevelopmentOrigin = "http://localhost:5173";

    /// <param name="configured">A comma-separated list. If it's empty, no browser origins are allowed.</param>
    /// <param name="isDevelopment">Adds the local Vite dev server.</param>
    public static string[] Parse(string? configured, bool isDevelopment)
    {
        var origins = (configured ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'))
            .Where(origin => origin != "*"); // "*" would mean any origin, which the design rules out.

        return (isDevelopment ? origins.Append(DevelopmentOrigin) : origins).Distinct().ToArray();
    }
}
