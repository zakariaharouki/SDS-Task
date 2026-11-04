using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public sealed class HostingBehaviourTests : IDisposable
{
    private readonly List<IDisposable> _factories = [];

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
    }

    // The https_port setting gives the redirect a port to use. Without it, the redirect does nothing,
    // and a test couldn't tell whether it was switched on.
    private HttpClient CreateClient(string environment, string address = "http://localhost")
    {
        var baseFactory = new SchemeApiFactory();
        var factory = baseFactory.WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            host.UseSetting("https_port", "443");
        });

        _factories.Add(factory);
        _factories.Add(baseFactory);

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(address)
        });
    }

    [Theory]
    [InlineData("Production", "http://localhost")]
    [InlineData("Development", "https://localhost")]
    public async Task Swagger_is_available(string environment, string address)
    {
        var client = CreateClient(environment, address);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/schemes")]
    public async Task Production_does_not_redirect_to_https(string path)
    {
        var response = await CreateClient("Production").GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Development_still_redirects_to_https()
    {
        var response = await CreateClient("Development").GetAsync("/api/schemes");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
    }
}
