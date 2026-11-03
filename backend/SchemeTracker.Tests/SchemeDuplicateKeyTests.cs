using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Services;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public class SchemeDuplicateKeyTests
{
    [Fact]
    public void Key_matches_the_design_example() =>
        Assert.Equal(
            "riverside phase 2|unit 4, riverside road, leeds ls1 4ab",
            SchemeDuplicateKey.Create("Riverside Phase 2", "Unit 4, Riverside Road, Leeds LS1 4AB"));

    [Theory]
    [InlineData("Riverside Phase 2", "riverside phase 2")]
    [InlineData("Riverside Phase 2", "RIVERSIDE PHASE 2")]
    [InlineData("Riverside Phase 2", "  Riverside Phase 2  ")]
    [InlineData("Riverside Phase 2", "Riverside  Phase   2")]
    [InlineData("Riverside Phase 2", "Riverside\tPhase\n2")]
    public void Case_and_spacing_differences_in_the_name_give_the_same_key(string first, string second) =>
        Assert.Equal(SchemeDuplicateKey.Create(first, "Leeds"), SchemeDuplicateKey.Create(second, "Leeds"));

    [Theory]
    [InlineData("Unit 4, Leeds", "unit 4, leeds")]
    [InlineData("Unit 4, Leeds", "  Unit  4,   Leeds ")]
    public void Case_and_spacing_differences_in_the_address_give_the_same_key(string first, string second) =>
        Assert.Equal(SchemeDuplicateKey.Create("Riverside", first), SchemeDuplicateKey.Create("Riverside", second));

    [Theory]
    [InlineData("Riverside Rd", "Riverside Road")]
    [InlineData("Unit 4, Leeds", "Unit 4 Leeds")]
    public void Different_wording_and_punctuation_stay_different(string first, string second) =>
        Assert.NotEqual(SchemeDuplicateKey.Create("Riverside", first), SchemeDuplicateKey.Create("Riverside", second));

    [Fact]
    public void Same_name_at_a_different_address_is_a_different_key() =>
        Assert.NotEqual(SchemeDuplicateKey.Create("The Old Depot", "Leeds"), SchemeDuplicateKey.Create("The Old Depot", "York"));

    [Fact]
    public void Different_name_at_the_same_address_is_a_different_key() =>
        Assert.NotEqual(SchemeDuplicateKey.Create("Phase 1", "Leeds"), SchemeDuplicateKey.Create("Phase 2", "Leeds"));
}

public class SchemeDuplicateKeyApiTests : IClassFixture<SchemeApiFactory>
{
    private const string Line = """{"homes":10,"averageValue":100.00}""";

    private const string Mix =
        "{\"socialRent\":" + Line +
        ",\"affordableRent\":" + Line +
        ",\"sharedOwnership\":" + Line +
        ",\"marketSale\":" + Line + "}";

    private readonly HttpClient _client;
    private readonly FakeSchemeRepository _repository;

    public SchemeDuplicateKeyApiTests(SchemeApiFactory factory)
    {
        _client = factory.CreateClient();
        _repository = (FakeSchemeRepository)factory.Services.GetRequiredService<ISchemeRepository>();
    }

    private static StringContent Json(string name, string address, string extra = "") => new(
        "{\"name\":" + JsonSerializer.Serialize(name) +
        ",\"siteAddress\":" + JsonSerializer.Serialize(address) +
        ",\"status\":\"Planning\",\"tenureMix\":" + Mix + extra + "}",
        Encoding.UTF8,
        "application/json");

    private async Task<string> CreateAsync(string name, string address)
    {
        var response = await _client.PostAsync("/api/schemes", Json(name, address));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> UpdateAsync(string id, string name, string address, int version) =>
        _client.PutAsync("/api/schemes/" + id, Json(name, address, ",\"version\":" + version));

    [Fact]
    public async Task Create_stores_the_normalised_key()
    {
        var id = await CreateAsync("  Riverside  Phase 2 ", " Unit 4,  Leeds ");

        Assert.Equal("riverside phase 2|unit 4, leeds", _repository.Stored(id).DuplicateKey);
    }

    [Fact]
    public async Task Normalising_the_key_does_not_change_what_the_user_typed()
    {
        var id = await CreateAsync("Riverside  Phase 2", "Unit 4, LEEDS");

        Assert.Equal("Riverside  Phase 2", _repository.Stored(id).Name);
        Assert.Equal("Unit 4, LEEDS", _repository.Stored(id).SiteAddress);
    }

    [Fact]
    public async Task Update_recalculates_the_key()
    {
        var id = await CreateAsync("Riverside", "Leeds");

        var response = await UpdateAsync(id, "  New  Name ", "York", version: 1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("new name|york", _repository.Stored(id).DuplicateKey);
    }

    [Fact]
    public async Task Update_with_the_same_name_and_address_keeps_the_same_key()
    {
        var id = await CreateAsync("Riverside", "Leeds");

        var response = await UpdateAsync(id, "Riverside", "Leeds", version: 1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("riverside|leeds", _repository.Stored(id).DuplicateKey);
    }
}
