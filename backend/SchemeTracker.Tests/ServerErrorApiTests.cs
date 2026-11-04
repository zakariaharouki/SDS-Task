using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

/// <summary>Runs as production, with a repository that always fails.</summary>
public class ProductionFailureFactory : SchemeApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseEnvironment("Production");
        builder.ConfigureTestServices(services =>
            services.AddSingleton<ISchemeRepository, FailingSchemeRepository>());
    }
}

public class FailingSchemeRepository : ISchemeRepository
{
    public const string SecretDetail = "internal-database-host";

    private static InvalidOperationException Failure() => new("Could not reach " + SecretDetail);

    public Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default) =>
        throw Failure();

    public Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        throw Failure();

    public Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default) =>
        throw Failure();

    public Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default) =>
        throw Failure();

    public Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default) =>
        throw Failure();
}

public class ServerErrorApiTests : IClassFixture<ProductionFailureFactory>
{
    private const string Line = """{"homes":10,"averageValue":100.00}""";

    private const string ValidBody =
        "{\"name\":\"A\",\"siteAddress\":\"B\",\"status\":\"Planning\",\"tenureMix\":" +
        "{\"socialRent\":" + Line + ",\"affordableRent\":" + Line + ",\"sharedOwnership\":" + Line + ",\"marketSale\":" + Line + "}}";

    private readonly HttpClient _client;

    public ServerErrorApiTests(ProductionFailureFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static async Task AssertGenericServerErrorAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));

        Assert.DoesNotContain(FailingSchemeRepository.SecretDetail, text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("SchemeTracker", text);
        Assert.DoesNotContain("   at ", text);
    }

    [Theory]
    [InlineData("/api/schemes")]
    [InlineData("/api/schemes/summary")]
    [InlineData("/api/schemes/6725f0a1b2c3d4e5f6a7b8c9")]
    public async Task Reads_return_a_generic_error(string path) =>
        await AssertGenericServerErrorAsync(await _client.GetAsync(path));

    [Fact]
    public async Task Writes_return_a_generic_error() =>
        await AssertGenericServerErrorAsync(
            await _client.PostAsync("/api/schemes", new StringContent(ValidBody, Encoding.UTF8, "application/json")));

    [Fact]
    public async Task Validation_errors_are_still_400_in_production()
    {
        var response = await _client.PostAsync(
            "/api/schemes",
            new StringContent(ValidBody.Replace("\"name\":\"A\"", "\"name\":\" \""), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
