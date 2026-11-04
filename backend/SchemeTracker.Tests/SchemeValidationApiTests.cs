using System.Net;
using System.Text;
using System.Text.Json;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public class SchemeValidationApiTests : IClassFixture<SchemeApiFactory>
{
    private readonly HttpClient _client;

    public SchemeValidationApiTests(SchemeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string Mix(int homes = 10, string averageValue = "100.00")
    {
        var line = "{\"homes\":" + homes + ",\"averageValue\":" + averageValue + "}";
        return "{\"socialRent\":" + line + ",\"affordableRent\":" + line + ",\"sharedOwnership\":" + line + ",\"marketSale\":" + line + "}";
    }

    private static string Fields(string name = "Riverside", string address = "Leeds", string status = "Planning", string? mix = null) =>
        "\"name\":" + JsonSerializer.Serialize(name) +
        ",\"siteAddress\":" + JsonSerializer.Serialize(address) +
        ",\"status\":\"" + status + "\",\"tenureMix\":" + (mix ?? Mix());

    private Task<HttpResponseMessage> PostAsync(string fields) =>
        _client.PostAsync("/api/schemes", new StringContent("{" + fields + "}", Encoding.UTF8, "application/json"));

    private Task<HttpResponseMessage> PutAsync(string id, string fields) =>
        _client.PutAsync("/api/schemes/" + id, new StringContent("{" + fields + ",\"version\":1}", Encoding.UTF8, "application/json"));

    private static async Task AssertErrorAsync(HttpResponseMessage response, string field, string message)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out var errors), $"No error for '{field}' in: {text}");
        Assert.Contains(message, errors.EnumerateArray().Select(error => error.GetString()));
    }

    [Fact]
    public async Task Valid_scheme_is_created_with_a_trimmed_name_and_address()
    {
        var response = await PostAsync(Fields(name: "  Riverside  ", address: " Leeds "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Riverside", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("Leeds", json.RootElement.GetProperty("siteAddress").GetString());
    }

    [Fact]
    public async Task Timestamps_are_whole_milliseconds_to_match_what_mongodb_stores()
    {
        var response = await PostAsync(Fields(name: "Timestamps " + Guid.NewGuid()));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("createdAt").GetDateTime().Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(0, json.RootElement.GetProperty("updatedAt").GetDateTime().Ticks % TimeSpan.TicksPerMillisecond);
    }

    [Fact]
    public async Task Maximum_values_do_not_overflow()
    {
        var response = await PostAsync(Fields(mix: Mix(homes: 10_000, averageValue: "10000000")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(40_000, json.RootElement.GetProperty("totalHomes").GetInt32());
        Assert.Equal(400_000_000_000m, json.RootElement.GetProperty("totalValue").GetDecimal());
    }

    [Fact]
    public async Task Blank_name_returns_its_field_error() =>
        await AssertErrorAsync(await PostAsync(Fields(name: "   ")), "name", "Name is required.");

    [Fact]
    public async Task Long_address_returns_its_field_error() =>
        await AssertErrorAsync(await PostAsync(Fields(address: new string('x', 301))), "siteAddress", "Site address must be 300 characters or fewer.");

    [Fact]
    public async Task Out_of_range_homes_return_a_nested_camel_case_path() =>
        await AssertErrorAsync(await PostAsync(Fields(mix: Mix(homes: -5))), "tenureMix.marketSale.homes", "Homes must be between 0 and 10,000.");

    [Fact]
    public async Task Zero_average_value_with_homes_returns_the_design_message() =>
        await AssertErrorAsync(await PostAsync(Fields(mix: Mix(averageValue: "0"))), "tenureMix.marketSale.averageValue", "Average value must be greater than £0 when there are homes.");

    [Fact]
    public async Task Three_decimal_places_are_rejected() =>
        await AssertErrorAsync(await PostAsync(Fields(mix: Mix(averageValue: "100.123"))), "tenureMix.socialRent.averageValue", "Average value can have at most 2 decimal places.");

    [Fact]
    public async Task Zero_homes_past_appraisal_returns_the_error_on_tenure_mix() =>
        await AssertErrorAsync(await PostAsync(Fields(mix: Mix(homes: 0, averageValue: "0"))), "tenureMix", "A scheme in Planning, On site or Complete must have at least 1 home.");

    [Fact]
    public async Task Zero_homes_at_appraisal_is_created_with_a_null_percent()
    {
        var response = await PostAsync(Fields(status: "Appraisal", mix: Mix(homes: 0, averageValue: "0")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("affordablePercent").ValueKind);
    }

    [Fact]
    public async Task All_field_errors_are_returned_together()
    {
        var response = await PostAsync(Fields(name: " ", address: " ", mix: Mix(homes: -1)));

        await AssertErrorAsync(response, "name", "Name is required.");
        await AssertErrorAsync(response, "siteAddress", "Site address is required.");
        await AssertErrorAsync(response, "tenureMix.socialRent.homes", "Homes must be between 0 and 10,000.");
    }

    [Fact]
    public async Task Update_validates_before_looking_up_the_scheme()
    {
        // The fake repository finds nothing, so a valid body would return 404.
        var unknownId = new string('f', 24);

        await AssertErrorAsync(await PutAsync(unknownId, Fields(name: " ")), "name", "Name is required.");
        Assert.Equal(HttpStatusCode.NotFound, (await PutAsync(unknownId, Fields())).StatusCode);
    }

    [Fact]
    public async Task Update_applies_the_homes_rule() =>
        await AssertErrorAsync(await PutAsync(new string('f', 24), Fields(mix: Mix(homes: 0, averageValue: "0"))), "tenureMix", "A scheme in Planning, On site or Complete must have at least 1 home.");
}
