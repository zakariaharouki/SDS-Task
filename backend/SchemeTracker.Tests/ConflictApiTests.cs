using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public class ConflictApiTests : IClassFixture<SchemeApiFactory>
{
    private const string Line = """{"homes":10,"averageValue":100.00}""";

    private const string Mix =
        "{\"socialRent\":" + Line +
        ",\"affordableRent\":" + Line +
        ",\"sharedOwnership\":" + Line +
        ",\"marketSale\":" + Line + "}";

    private const string DuplicateTitle = "This scheme already exists.";
    private const string DuplicateMessage = "A scheme with this name already exists at this address.";
    private const string VersionTitle = "This scheme was changed by someone else.";
    private const string VersionMessage = "This scheme was changed by someone else. Reload to see the latest version.";

    private readonly HttpClient _client;
    private readonly FakeSchemeRepository _repository;

    public ConflictApiTests(SchemeApiFactory factory)
    {
        _client = factory.CreateClient();
        _repository = (FakeSchemeRepository)factory.Services.GetRequiredService<ISchemeRepository>();
        _repository.EnforceUniqueKeys = true;
    }

    private static string UniqueName() => "Scheme " + Guid.NewGuid();

    private static StringContent Json(string name, string address, string extra = "") => new(
        "{\"name\":" + JsonSerializer.Serialize(name) +
        ",\"siteAddress\":" + JsonSerializer.Serialize(address) +
        ",\"status\":\"Planning\",\"tenureMix\":" + Mix + extra + "}",
        Encoding.UTF8,
        "application/json");

    private Task<HttpResponseMessage> PostAsync(string name, string address) =>
        _client.PostAsync("/api/schemes", Json(name, address));

    private Task<HttpResponseMessage> PutAsync(string id, string name, string address, int version) =>
        _client.PutAsync("/api/schemes/" + id, Json(name, address, ",\"version\":" + version));

    private async Task<string> CreateAsync(string name, string address)
    {
        var response = await PostAsync(name, address);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task AssertDuplicateAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(DuplicateTitle, json.RootElement.GetProperty("title").GetString());

        var errors = json.RootElement.GetProperty("errors");
        Assert.Equal(DuplicateMessage, errors.GetProperty("name")[0].GetString());
        Assert.Equal(DuplicateMessage, errors.GetProperty("siteAddress")[0].GetString());
    }

    private static async Task AssertVersionConflictAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(VersionTitle, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(VersionMessage, json.RootElement.GetProperty("detail").GetString());
        Assert.False(json.RootElement.TryGetProperty("errors", out _), $"Unexpected field errors in: {text}");
    }

    [Fact]
    public async Task Same_name_and_address_is_rejected()
    {
        var name = UniqueName();
        await CreateAsync(name, "Leeds");

        await AssertDuplicateAsync(await PostAsync(name, "Leeds"));
    }

    [Fact]
    public async Task Case_and_spacing_differences_are_still_a_duplicate()
    {
        var name = UniqueName();
        await CreateAsync(name, "Unit 4, Leeds");

        await AssertDuplicateAsync(await PostAsync("  " + name.ToUpperInvariant() + " ", "unit  4,  LEEDS"));
    }

    [Fact]
    public async Task Same_name_at_a_different_address_is_allowed()
    {
        var name = UniqueName();
        await CreateAsync(name, "Leeds");

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(name, "York")).StatusCode);
    }

    [Fact]
    public async Task Different_name_at_the_same_address_is_allowed()
    {
        var address = "Address " + Guid.NewGuid();
        await CreateAsync(UniqueName(), address);

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(UniqueName(), address)).StatusCode);
    }

    [Fact]
    public async Task Update_keeping_its_own_name_and_address_does_not_clash_with_itself()
    {
        var name = UniqueName();
        var id = await CreateAsync(name, "Leeds");

        Assert.Equal(HttpStatusCode.OK, (await PutAsync(id, name, "Leeds", version: 1)).StatusCode);
    }

    [Fact]
    public async Task Update_onto_another_schemes_name_and_address_is_rejected()
    {
        var takenName = UniqueName();
        await CreateAsync(takenName, "Leeds");
        var id = await CreateAsync(UniqueName(), "York");

        await AssertDuplicateAsync(await PutAsync(id, takenName, "Leeds", version: 1));
    }

    [Fact]
    public async Task Rejected_update_leaves_the_stored_scheme_unchanged()
    {
        var takenName = UniqueName();
        await CreateAsync(takenName, "Leeds");
        var name = UniqueName();
        var id = await CreateAsync(name, "York");

        await PutAsync(id, takenName, "Leeds", version: 1);

        Assert.Equal(name, _repository.Stored(id).Name);
        Assert.Equal(1, _repository.Stored(id).Version);
    }

    [Fact]
    public async Task Stale_version_is_a_conflict_not_a_404()
    {
        var name = UniqueName();
        var id = await CreateAsync(name, "Leeds");

        await AssertVersionConflictAsync(await PutAsync(id, name, "Leeds", version: 99));
    }

    [Fact]
    public async Task Second_save_from_the_same_loaded_version_is_a_conflict()
    {
        var name = UniqueName();
        var id = await CreateAsync(name, "Leeds");

        Assert.Equal(HttpStatusCode.OK, (await PutAsync(id, name, "Leeds", version: 1)).StatusCode);

        await AssertVersionConflictAsync(await PutAsync(id, name, "York", version: 1));
        Assert.Equal("Leeds", _repository.Stored(id).SiteAddress);
    }

    [Fact]
    public async Task Successful_update_increments_the_version()
    {
        var name = UniqueName();
        var id = await CreateAsync(name, "Leeds");

        var response = await PutAsync(id, name, "Leeds", version: 1);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Unknown_id_is_still_a_404()
    {
        var response = await PutAsync(new string('f', 24), UniqueName(), "Leeds", version: 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
