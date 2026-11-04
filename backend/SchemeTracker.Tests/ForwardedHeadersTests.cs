using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public sealed class ForwardedHeadersTests : IDisposable
{
    private readonly SchemeApiFactory _baseFactory = new();
    private readonly WebApplicationFactory<Program> _factory;

    public ForwardedHeadersTests()
    {
        _factory = _baseFactory.WithWebHostBuilder(host => host.UseEnvironment("Production"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _baseFactory.Dispose();
    }

    private static object Line => new { homes = 10, averageValue = 100m };

    private async Task<Uri?> CreateAsync(string? forwardedProto)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/schemes")
        {
            Content = JsonContent.Create(new
            {
                name = "Riverside",
                siteAddress = "Leeds",
                status = "Planning",
                tenureMix = new { socialRent = Line, affordableRent = Line, sharedOwnership = Line, marketSale = Line }
            })
        };

        if (forwardedProto is not null)
        {
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        }

        using var response = await _factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.Headers.Location;
    }

    [Fact]
    public async Task Location_uses_https_when_the_proxy_says_the_request_was_https() =>
        Assert.Equal("https", (await CreateAsync("https"))!.Scheme);

    [Fact]
    public async Task Location_stays_http_without_the_header() =>
        Assert.Equal("http", (await CreateAsync(null))!.Scheme);
}
