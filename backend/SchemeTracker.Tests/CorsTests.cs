using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public sealed class CorsTests : IDisposable
{
    private const string VercelOrigin = "https://scheme-tracker.vercel.app";
    private const string LocalOrigin = "http://localhost:5173";
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private readonly List<IDisposable> _factories = [];

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
    }

    private HttpClient CreateClient(string environment, string? allowedOrigins)
    {
        var baseFactory = new SchemeApiFactory();
        var factory = baseFactory.WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = allowedOrigins
            }));
        });

        _factories.Add(factory);
        _factories.Add(baseFactory);
        return factory.CreateClient();
    }

    private static async Task<string?> AllowedOriginAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/schemes");
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);
        return response.Headers.TryGetValues(AllowOriginHeader, out var values) ? values.Single() : null;
    }

    private static async Task<HttpResponseMessage> PreflightAsync(HttpClient client, string origin, string method)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/schemes");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Configured_origin_is_allowed() =>
        Assert.Equal(VercelOrigin, await AllowedOriginAsync(CreateClient("Production", VercelOrigin), VercelOrigin));

    [Fact]
    public async Task Other_origins_are_not_allowed() =>
        Assert.Null(await AllowedOriginAsync(CreateClient("Production", VercelOrigin), "https://evil.example.com"));

    [Fact]
    public async Task Several_origins_can_be_configured_and_a_trailing_slash_is_ignored()
    {
        var client = CreateClient("Production", $"{VercelOrigin}/ , https://other.example.com");

        Assert.Equal(VercelOrigin, await AllowedOriginAsync(client, VercelOrigin));
        Assert.Equal("https://other.example.com", await AllowedOriginAsync(client, "https://other.example.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task With_no_origins_configured_the_api_still_runs_and_allows_no_browser_origins(string? allowedOrigins)
    {
        var client = CreateClient("Production", allowedOrigins);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Null(await AllowedOriginAsync(client, VercelOrigin));
    }

    [Fact]
    public async Task A_wildcard_never_allows_every_origin() =>
        Assert.Null(await AllowedOriginAsync(CreateClient("Production", "*"), "https://evil.example.com"));

    [Fact]
    public async Task Local_dev_server_is_allowed_in_development() =>
        Assert.Equal(LocalOrigin, await AllowedOriginAsync(CreateClient("Development", null), LocalOrigin));

    [Fact]
    public async Task Local_dev_server_is_not_allowed_in_production() =>
        Assert.Null(await AllowedOriginAsync(CreateClient("Production", VercelOrigin), LocalOrigin));

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Preflight_allows_the_methods_the_api_uses(string method)
    {
        using var response = await PreflightAsync(CreateClient("Production", VercelOrigin), VercelOrigin, method);

        Assert.Equal(VercelOrigin, response.Headers.GetValues(AllowOriginHeader).Single());
        Assert.Contains(method, response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("content-type", response.Headers.GetValues("Access-Control-Allow-Headers").Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_does_not_allow_delete_because_the_api_has_none()
    {
        using var response = await PreflightAsync(CreateClient("Production", VercelOrigin), VercelOrigin, "DELETE");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Methods")
            && response.Headers.GetValues("Access-Control-Allow-Methods").Single().Contains("DELETE"));
    }
}
