using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

/// <summary>HTTP requests through the whole API to real MongoDB (design section 7.2).</summary>
[Collection(MongoCollection.Name)]
public sealed class SchemeApiIntegrationTests : IDisposable
{
    private const string SchemesPath = "/api/schemes";
    private const string SummaryPath = "/api/schemes/summary";

    private readonly MongoFixture _mongo;
    private readonly List<MongoApiFactory> _factories = [];

    public SchemeApiIntegrationTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
    }

    private HttpClient CreateClient(TimeProvider? clock = null)
    {
        var factory = new MongoApiFactory(_mongo.ConnectionString, clock);
        _factories.Add(factory);
        return factory.CreateClient();
    }

    private static object Line(int homes, decimal averageValue = 100m) =>
        new { homes, averageValue };

    private static object Mix(int socialRent = 10, int affordableRent = 10, int sharedOwnership = 10, int marketSale = 10) => new
    {
        socialRent = Line(socialRent),
        affordableRent = Line(affordableRent),
        sharedOwnership = Line(sharedOwnership),
        marketSale = Line(marketSale)
    };

    private static object CreateBody(string name = "Riverside", string address = "Leeds", string status = "Planning", object? mix = null) =>
        new { name, siteAddress = address, status, tenureMix = mix ?? Mix() };

    private static object UpdateBody(int version, string name = "Riverside", string address = "Leeds", string status = "Planning", object? mix = null) =>
        new { name, siteAddress = address, status, tenureMix = mix ?? Mix(), version };

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<JsonElement> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync(SchemesPath, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync(response);
    }

    private static async Task<List<string?>> ListNamesAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync(SchemesPath + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schemes = await ReadAsync(response);
        return schemes.EnumerateArray().Select(scheme => scheme.GetProperty("name").GetString()).ToList();
    }

    private static string Id(JsonElement scheme) => scheme.GetProperty("id").GetString()!;

    private static async Task AssertDuplicateAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await ReadAsync(response);
        Assert.Equal("This scheme already exists.", problem.GetProperty("title").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("name", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("siteAddress", out _));
    }

    [MongoFact]
    public async Task Create_returns_201_with_a_working_location_and_get_returns_the_same_scheme()
    {
        var client = CreateClient();
        var mix = new
        {
            socialRent = Line(40, 150000m),
            affordableRent = Line(30, 180000m),
            sharedOwnership = Line(20, 220000m),
            marketSale = Line(60, 350000m)
        };

        var created = await client.PostAsJsonAsync(SchemesPath, CreateBody(mix: mix));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await ReadAsync(created);

        var fetched = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

        // The whole response must match, because the frontend treats both as the same scheme.
        Assert.Equal(body.ToString(), (await ReadAsync(fetched)).ToString());

        Assert.Equal(150, body.GetProperty("totalHomes").GetInt32());
        Assert.Equal(90, body.GetProperty("affordableHomes").GetInt32());
        Assert.Equal(60.0m, body.GetProperty("affordablePercent").GetDecimal());
        Assert.Equal(36_800_000m, body.GetProperty("totalValue").GetDecimal());
        Assert.Equal(1, body.GetProperty("version").GetInt32());
        Assert.Equal("Planning", body.GetProperty("status").GetString());
    }

    [MongoFact]
    public async Task Money_comes_back_exactly_through_the_api()
    {
        var client = CreateClient();
        var mix = new
        {
            socialRent = Line(1, 123456.78m),
            affordableRent = Line(3, 0.10m),
            sharedOwnership = Line(0, 0m),
            marketSale = Line(0, 0m)
        };

        var created = await CreateAsync(client, CreateBody(mix: mix));
        var fetched = await ReadAsync(await client.GetAsync($"{SchemesPath}/{Id(created)}"));

        Assert.Equal(123456.78m, fetched.GetProperty("tenureMix").GetProperty("socialRent").GetProperty("averageValue").GetDecimal());
        Assert.Equal(0.10m, fetched.GetProperty("tenureMix").GetProperty("affordableRent").GetProperty("averageValue").GetDecimal());
        Assert.Equal(123457.08m, fetched.GetProperty("totalValue").GetDecimal());
    }

    [MongoFact]
    public async Task Duplicate_with_different_case_and_spacing_is_a_409()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "Riverside Phase 2", address: "Unit 4, Leeds"));

        var response = await client.PostAsJsonAsync(SchemesPath, CreateBody(name: "  riverside  PHASE 2 ", address: "unit 4,  LEEDS"));

        await AssertDuplicateAsync(response);
    }

    [MongoFact]
    public async Task Same_name_at_a_different_address_is_created()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(address: "Leeds"));

        var response = await client.PostAsJsonAsync(SchemesPath, CreateBody(address: "York"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [MongoFact]
    public async Task Two_simultaneous_duplicates_give_one_201_and_one_409()
    {
        var client = CreateClient();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(SchemesPath, CreateBody()),
            client.PostAsJsonAsync(SchemesPath, CreateBody()));

        var statuses = responses.Select(response => response.StatusCode).ToList();
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);

        Assert.Equal(["Riverside"], await ListNamesAsync(client, string.Empty));
    }

    [MongoFact]
    public async Task Get_returns_404_for_an_unknown_id() =>
        Assert.Equal(HttpStatusCode.NotFound, (await CreateClient().GetAsync($"{SchemesPath}/6725f0a1b2c3d4e5f6a7b8c9")).StatusCode);

    [MongoFact]
    public async Task Get_returns_404_for_a_malformed_id() =>
        Assert.Equal(HttpStatusCode.NotFound, (await CreateClient().GetAsync($"{SchemesPath}/not-an-objectid")).StatusCode);

    [MongoFact]
    public async Task Update_returns_404_for_a_malformed_id()
    {
        var response = await CreateClient().PutAsJsonAsync($"{SchemesPath}/not-an-objectid", UpdateBody(version: 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [MongoFact]
    public async Task Update_changes_updated_at_keeps_created_at_and_bumps_the_version()
    {
        var clock = new TestClock();
        var client = CreateClient(clock);
        var created = await CreateAsync(client, CreateBody());

        clock.Now = clock.Now.AddDays(1);
        var response = await client.PutAsJsonAsync($"{SchemesPath}/{Id(created)}", UpdateBody(version: 1, name: "Renamed", status: "OnSite"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await ReadAsync(await client.GetAsync($"{SchemesPath}/{Id(created)}"));
        Assert.Equal("Renamed", updated.GetProperty("name").GetString());
        Assert.Equal("OnSite", updated.GetProperty("status").GetString());
        Assert.Equal(2, updated.GetProperty("version").GetInt32());
        Assert.Equal(created.GetProperty("createdAt").GetDateTime(), updated.GetProperty("createdAt").GetDateTime());
        Assert.Equal(clock.Now.UtcDateTime, updated.GetProperty("updatedAt").GetDateTime());
    }

    [MongoFact]
    public async Task Update_recalculates_the_stored_figures()
    {
        var client = CreateClient();
        var created = await CreateAsync(client, CreateBody());

        await client.PutAsJsonAsync($"{SchemesPath}/{Id(created)}", UpdateBody(version: 1, mix: Mix(socialRent: 1, affordableRent: 0, sharedOwnership: 0, marketSale: 1)));

        var updated = await ReadAsync(await client.GetAsync($"{SchemesPath}/{Id(created)}"));
        Assert.Equal(2, updated.GetProperty("totalHomes").GetInt32());
        Assert.Equal(1, updated.GetProperty("affordableHomes").GetInt32());
        Assert.Equal(50.0m, updated.GetProperty("affordablePercent").GetDecimal());
        Assert.Equal(200m, updated.GetProperty("totalValue").GetDecimal());
    }

    [MongoFact]
    public async Task Stale_version_is_a_409_and_changes_nothing()
    {
        var client = CreateClient();
        var created = await CreateAsync(client, CreateBody());
        var path = $"{SchemesPath}/{Id(created)}";
        await client.PutAsJsonAsync(path, UpdateBody(version: 1, name: "First"));

        var response = await client.PutAsJsonAsync(path, UpdateBody(version: 1, name: "Second"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("This scheme was changed by someone else.", (await ReadAsync(response)).GetProperty("title").GetString());
        Assert.Equal("First", (await ReadAsync(await client.GetAsync(path))).GetProperty("name").GetString());
    }

    [MongoFact]
    public async Task Update_keeping_its_own_name_and_address_is_allowed()
    {
        var client = CreateClient();
        var created = await CreateAsync(client, CreateBody());

        var response = await client.PutAsJsonAsync($"{SchemesPath}/{Id(created)}", UpdateBody(version: 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [MongoFact]
    public async Task Update_onto_another_schemes_name_and_address_is_a_409()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "Taken", address: "Leeds"));
        var created = await CreateAsync(client, CreateBody(name: "Free", address: "York"));

        var response = await client.PutAsJsonAsync($"{SchemesPath}/{Id(created)}", UpdateBody(version: 1, name: "taken", address: "LEEDS"));

        await AssertDuplicateAsync(response);
    }

    [MongoFact]
    public async Task Zero_homes_cannot_move_to_planning_but_can_move_back_to_appraisal()
    {
        var client = CreateClient();
        var noHomes = Mix(0, 0, 0, 0);
        var appraisal = await CreateAsync(client, CreateBody(name: "Early", status: "Appraisal", mix: noHomes));

        var toPlanning = await client.PutAsJsonAsync($"{SchemesPath}/{Id(appraisal)}", UpdateBody(version: 1, name: "Early", status: "Planning", mix: noHomes));
        Assert.Equal(HttpStatusCode.BadRequest, toPlanning.StatusCode);
        Assert.True((await ReadAsync(toPlanning)).GetProperty("errors").TryGetProperty("tenureMix", out _));

        var planning = await CreateAsync(client, CreateBody(name: "Later", status: "Planning"));
        var toAppraisal = await client.PutAsJsonAsync($"{SchemesPath}/{Id(planning)}", UpdateBody(version: 1, name: "Later", status: "Appraisal", mix: noHomes));
        Assert.Equal(HttpStatusCode.OK, toAppraisal.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadAsync(toAppraisal)).GetProperty("affordablePercent").ValueKind);
    }

    [MongoFact]
    public async Task List_sorts_by_status_in_pipeline_order_by_default_and_in_reverse()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "C", status: "Complete"));
        await CreateAsync(client, CreateBody(name: "O", status: "OnSite"));
        await CreateAsync(client, CreateBody(name: "A", status: "Appraisal"));
        await CreateAsync(client, CreateBody(name: "P", status: "Planning"));

        Assert.Equal(["A", "P", "O", "C"], await ListNamesAsync(client, string.Empty));
        Assert.Equal(["C", "O", "P", "A"], await ListNamesAsync(client, "?sortDir=desc"));
    }

    [MongoFact]
    public async Task List_filters_by_several_statuses()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "A", status: "Appraisal"));
        await CreateAsync(client, CreateBody(name: "P", status: "Planning"));
        await CreateAsync(client, CreateBody(name: "O", status: "OnSite"));

        Assert.Equal(["P", "O"], await ListNamesAsync(client, "?status=Planning&status=OnSite"));
    }

    [MongoFact]
    public async Task List_sorts_by_value_and_includes_figures()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "Big", mix: Mix(100, 100, 100, 100)));
        await CreateAsync(client, CreateBody(name: "Small", mix: Mix(1, 1, 1, 1)));

        Assert.Equal(["Big", "Small"], await ListNamesAsync(client, "?sortBy=totalValue&sortDir=desc"));

        var schemes = await ReadAsync(await client.GetAsync(SchemesPath + "?sortBy=totalValue"));
        var first = schemes[0];
        Assert.Equal("Small", first.GetProperty("name").GetString());
        Assert.Equal(4, first.GetProperty("totalHomes").GetInt32());
        Assert.Equal(75.0m, first.GetProperty("affordablePercent").GetDecimal());
        Assert.Equal(400m, first.GetProperty("totalValue").GetDecimal());
    }

    [MongoFact]
    public async Task Summary_of_an_empty_database_is_zeros_and_a_null_percent()
    {
        var response = await CreateClient().GetAsync(SummaryPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await ReadAsync(response);
        Assert.Equal(0, summary.GetProperty("schemeCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("totalHomes").GetInt32());
        Assert.Equal(0m, summary.GetProperty("totalValue").GetDecimal());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("affordablePercent").ValueKind);

        var rows = summary.GetProperty("byStatus").EnumerateArray().ToList();
        Assert.Equal(["Appraisal", "Planning", "OnSite", "Complete"], rows.Select(row => row.GetProperty("status").GetString()));
        Assert.All(rows, row => Assert.Equal(JsonValueKind.Null, row.GetProperty("affordablePercent").ValueKind));
    }

    [MongoFact]
    public async Task Summary_matches_the_sum_of_the_schemes_and_weights_the_percent_by_homes()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "Small", status: "Planning", mix: Mix(10, 0, 0, 0)));
        await CreateAsync(client, CreateBody(name: "Large", status: "OnSite", mix: Mix(0, 0, 0, 490)));

        var summary = await ReadAsync(await client.GetAsync(SummaryPath));

        Assert.Equal(2, summary.GetProperty("schemeCount").GetInt32());
        Assert.Equal(500, summary.GetProperty("totalHomes").GetInt32());
        Assert.Equal(10, summary.GetProperty("affordableHomes").GetInt32());
        Assert.Equal(50_000m, summary.GetProperty("totalValue").GetDecimal());

        // 10 of 500 homes is 2%. Averaging the schemes' own percentages would wrongly give 50%.
        Assert.Equal(2.0m, summary.GetProperty("affordablePercent").GetDecimal());

        var planning = summary.GetProperty("byStatus")[1];
        Assert.Equal("Planning", planning.GetProperty("status").GetString());
        Assert.Equal(1, planning.GetProperty("schemeCount").GetInt32());
        Assert.Equal(100.0m, planning.GetProperty("affordablePercent").GetDecimal());
    }

    [MongoFact]
    public async Task Summary_follows_the_status_filter()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody(name: "P", status: "Planning"));
        await CreateAsync(client, CreateBody(name: "O", status: "OnSite"));

        var summary = await ReadAsync(await client.GetAsync(SummaryPath + "?status=OnSite"));

        Assert.Equal(1, summary.GetProperty("schemeCount").GetInt32());
        Assert.Equal(4, summary.GetProperty("byStatus").GetArrayLength());
        Assert.Equal(0, summary.GetProperty("byStatus")[1].GetProperty("schemeCount").GetInt32());
        Assert.Equal(1, summary.GetProperty("byStatus")[2].GetProperty("schemeCount").GetInt32());
    }

    [MongoFact]
    public async Task Summary_route_is_not_treated_as_an_id()
    {
        var client = CreateClient();
        await CreateAsync(client, CreateBody());

        var summary = await ReadAsync(await client.GetAsync(SummaryPath));

        Assert.True(summary.TryGetProperty("byStatus", out _));
        Assert.False(summary.TryGetProperty("id", out _));
    }
}
