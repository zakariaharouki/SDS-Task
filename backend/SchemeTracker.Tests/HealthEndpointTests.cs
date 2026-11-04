using System.Net;

namespace SchemeTracker.Tests;

public class HealthEndpointTests : IClassFixture<ProductionFailureFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ProductionFailureFactory factory)
    {
        _client = factory.CreateClient();
    }

    // This factory's repository always fails, so a 200 shows the check doesn't depend on MongoDB.
    [Fact]
    public async Task Health_returns_200_without_touching_the_database()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
