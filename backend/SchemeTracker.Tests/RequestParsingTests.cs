using System.Net;
using System.Text;
using System.Text.Json;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public class RequestParsingTests : IClassFixture<SchemeApiFactory>
{
    private const string ValidLine = """{"homes":10,"averageValue":100.00}""";

    private const string ValidMix =
        "{\"socialRent\":" + ValidLine +
        ",\"affordableRent\":" + ValidLine +
        ",\"sharedOwnership\":" + ValidLine +
        ",\"marketSale\":" + ValidLine + "}";

    private readonly HttpClient _client;

    public RequestParsingTests(SchemeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string Body(string status = "\"Planning\"", string mix = ValidMix, string extra = "") =>
        "{\"name\":\"Riverside\",\"siteAddress\":\"Leeds\",\"status\":" + status +
        ",\"tenureMix\":" + mix + extra + "}";

    private Task<HttpResponseMessage> PostAsync(string body) =>
        _client.PostAsync("/api/schemes", new StringContent(body, Encoding.UTF8, "application/json"));

    private static async Task AssertBadRequestAsync(HttpResponseMessage response, string field, string message)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        var errors = json.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty(field, out var fieldErrors), $"No error for '{field}' in: {text}");
        Assert.Contains(message, fieldErrors.EnumerateArray().Select(error => error.GetString()));
        Assert.False(errors.TryGetProperty("request", out _), $"Unexpected 'request' error in: {text}");
    }

    [Fact]
    public async Task Valid_request_is_accepted()
    {
        var response = await PostAsync(Body());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Appraisal")]
    [InlineData("Planning")]
    [InlineData("OnSite")]
    [InlineData("Complete")]
    public async Task Each_exact_status_name_is_accepted(string status)
    {
        var response = await PostAsync(Body(status: $"\"{status}\""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("99")]
    [InlineData("1")]
    [InlineData("\"1\"")]
    [InlineData("\"planning\"")]
    [InlineData("\"ONSITE\"")]
    [InlineData("\"Done\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    public async Task Anything_but_an_exact_status_name_is_rejected(string status)
    {
        var response = await PostAsync(Body(status: status));

        await AssertBadRequestAsync(response, "status", "Status must be one of: Appraisal, Planning, OnSite, Complete.");
    }

    [Theory]
    [InlineData("id", "\"abc\"")]
    [InlineData("createdAt", "\"2020-01-01T00:00:00Z\"")]
    [InlineData("updatedAt", "\"2020-01-01T00:00:00Z\"")]
    [InlineData("totals", "{}")]
    [InlineData("totalHomes", "5")]
    [InlineData("duplicateKey", "\"x\"")]
    [InlineData("version", "1")]
    [InlineData("typo", "1")]
    public async Task Fields_a_create_request_does_not_define_are_rejected(string field, string value)
    {
        var response = await PostAsync(Body(extra: $",\"{field}\":{value}"));

        await AssertBadRequestAsync(response, field, "This field is not allowed.");
    }

    [Fact]
    public async Task Unknown_field_inside_a_tenure_is_reported_at_its_path()
    {
        var mix = ValidMix.Replace(
            "\"marketSale\":{\"homes\":10,",
            "\"marketSale\":{\"typo\":1,\"homes\":10,");

        var response = await PostAsync(Body(mix: mix));

        await AssertBadRequestAsync(response, "tenureMix.marketSale.typo", "This field is not allowed.");
    }

    [Fact]
    public async Task Decimal_homes_are_rejected_not_rounded()
    {
        var mix = ValidMix.Replace(
            "\"marketSale\":{\"homes\":10,",
            "\"marketSale\":{\"homes\":2.5,");

        var response = await PostAsync(Body(mix: mix));

        await AssertBadRequestAsync(response, "tenureMix.marketSale.homes", "Homes must be a whole number.");
    }

    [Fact]
    public async Task Non_numeric_average_value_is_rejected()
    {
        var mix = ValidMix.Replace(
            "\"marketSale\":{\"homes\":10,\"averageValue\":100.00}",
            "\"marketSale\":{\"homes\":10,\"averageValue\":\"lots\"}");

        var response = await PostAsync(Body(mix: mix));

        await AssertBadRequestAsync(response, "tenureMix.marketSale.averageValue", "Average value must be a number.");
    }

    [Fact]
    public async Task Missing_name_is_reported_as_required()
    {
        var response = await PostAsync("{\"siteAddress\":\"Leeds\",\"status\":\"Planning\",\"tenureMix\":" + ValidMix + "}");

        await AssertBadRequestAsync(response, "name", "Name is required.");
    }

    [Fact]
    public async Task Every_missing_field_at_the_same_level_is_reported()
    {
        var response = await PostAsync("{\"tenureMix\":" + ValidMix + "}");

        await AssertBadRequestAsync(response, "name", "Name is required.");
        await AssertBadRequestAsync(response, "siteAddress", "Site address is required.");
        await AssertBadRequestAsync(response, "status", "Status is required.");
    }

    [Fact]
    public async Task Missing_tenure_is_an_error_not_zero()
    {
        var mix = "{\"socialRent\":" + ValidLine + ",\"affordableRent\":" + ValidLine + ",\"sharedOwnership\":" + ValidLine + "}";

        var response = await PostAsync(Body(mix: mix));

        await AssertBadRequestAsync(response, "tenureMix.marketSale", "Market sale is required.");
    }

    [Fact]
    public async Task Null_name_is_reported_with_a_camel_case_field()
    {
        var response = await PostAsync("{\"name\":null,\"siteAddress\":\"Leeds\",\"status\":\"Planning\",\"tenureMix\":" + ValidMix + "}");

        await AssertBadRequestAsync(response, "name", "Name is required.");
    }

    [Fact]
    public async Task Null_tenure_is_reported_at_its_path()
    {
        var mix = ValidMix.Replace("\"marketSale\":" + ValidLine, "\"marketSale\":null");

        var response = await PostAsync(Body(mix: mix));

        await AssertBadRequestAsync(response, "tenureMix.marketSale", "Market sale is required.");
    }

    [Theory]
    [InlineData("{\"name\":")]
    [InlineData("{bad json}")]
    [InlineData("not json at all")]
    public async Task Malformed_json_gets_a_friendly_message(string body)
    {
        var response = await PostAsync(body);

        await AssertBadRequestAsync(response, "body", "The request body is not valid JSON.");
    }

    [Fact]
    public async Task Empty_body_is_rejected()
    {
        var response = await PostAsync(string.Empty);

        await AssertBadRequestAsync(response, "body", "A request body is required.");
    }

    [Theory]
    [InlineData("{\"name\":\"A\",\"siteAddress\":\"B\",\"status\":\"Done\",\"tenureMix\":{}}")]
    [InlineData("{\"name\":\"A\",\"siteAddress\":\"B\",\"status\":\"Planning\",\"tenureMix\":{\"socialRent\":{\"homes\":2.5,\"averageValue\":1}}}")]
    [InlineData("{\"typo\":1}")]
    [InlineData("{}")]
    [InlineData("{bad")]
    public async Task Responses_never_expose_framework_details(string body)
    {
        var text = await (await PostAsync(body)).Content.ReadAsStringAsync();

        Assert.DoesNotContain("System.", text);
        Assert.DoesNotContain("SchemeTracker.", text);
        Assert.DoesNotContain("JSON value", text);
        Assert.DoesNotContain("LineNumber", text);
    }

    [Fact]
    public async Task Update_requests_follow_the_same_rules()
    {
        var path = "/api/schemes/" + new string('a', 24);
        var valid = "{\"name\":\"A\",\"siteAddress\":\"B\",\"status\":\"Planning\",\"tenureMix\":" + ValidMix;

        var missingVersion = await _client.PutAsync(path, new StringContent(valid + "}", Encoding.UTF8, "application/json"));
        await AssertBadRequestAsync(missingVersion, "version", "Version is required.");

        var textVersion = await _client.PutAsync(path, new StringContent(valid + ",\"version\":\"x\"}", Encoding.UTF8, "application/json"));
        await AssertBadRequestAsync(textVersion, "version", "Version must be a whole number.");

        var sentId = await _client.PutAsync(path, new StringContent(valid + ",\"version\":1,\"id\":\"abc\"}", Encoding.UTF8, "application/json"));
        await AssertBadRequestAsync(sentId, "id", "This field is not allowed.");
    }
}
