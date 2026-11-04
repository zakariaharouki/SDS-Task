using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

public class QueryParameterApiTests : IClassFixture<SchemeApiFactory>
{
    private const string ListPath = "/api/schemes";
    private const string SummaryPath = "/api/schemes/summary";
    private const string StatusMessage = "Status must be one of: Appraisal, Planning, OnSite, Complete.";

    private readonly HttpClient _client;
    private readonly FakeSchemeRepository _repository;

    public QueryParameterApiTests(SchemeApiFactory factory)
    {
        _client = factory.CreateClient();
        _repository = (FakeSchemeRepository)factory.Services.GetRequiredService<ISchemeRepository>();
    }

    private static async Task<string[]> AssertBadRequestAsync(HttpResponseMessage response, string field)
    {
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(text);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out var errors), $"No error for '{field}' in: {text}");

        return errors.EnumerateArray().Select(error => error.GetString()!).ToArray();
    }

    [Fact]
    public async Task List_uses_the_design_defaults()
    {
        var response = await _client.GetAsync(ListPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("status", _repository.LastSortBy);
        Assert.Equal("asc", _repository.LastSortDir);
    }

    [Theory]
    [InlineData(ListPath)]
    [InlineData(SummaryPath)]
    public async Task No_status_filter_reaches_the_repository_as_null(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(_repository.LastStatuses);
    }

    [Theory]
    [InlineData(ListPath)]
    [InlineData(SummaryPath)]
    public async Task Repeated_status_values_all_reach_the_repository(string path)
    {
        var response = await _client.GetAsync(path + "?status=Planning&status=OnSite");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([SchemeStatus.Planning, SchemeStatus.OnSite], _repository.LastStatuses);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("name")]
    [InlineData("totalHomes")]
    [InlineData("totalValue")]
    [InlineData("updatedAt")]
    public async Task Each_sort_field_is_accepted(string sortBy)
    {
        var response = await _client.GetAsync($"{ListPath}?sortBy={sortBy}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sortBy, _repository.LastSortBy);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task Each_sort_direction_is_accepted(string sortDir)
    {
        var response = await _client.GetAsync($"{ListPath}?sortDir={sortDir}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sortDir, _repository.LastSortDir);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("Name")]
    [InlineData("TOTALHOMES")]
    [InlineData("totalhomes")]
    [InlineData("_id")]
    public async Task Invalid_sort_field_is_rejected(string sortBy)
    {
        var messages = await AssertBadRequestAsync(await _client.GetAsync($"{ListPath}?sortBy={sortBy}"), "sortBy");

        Assert.Equal(["Sort by must be one of: status, name, totalHomes, totalValue, updatedAt."], messages);
    }

    [Theory]
    [InlineData("sideways")]
    [InlineData("ASC")]
    [InlineData("Desc")]
    public async Task Invalid_sort_direction_is_rejected(string sortDir)
    {
        var messages = await AssertBadRequestAsync(await _client.GetAsync($"{ListPath}?sortDir={sortDir}"), "sortDir");

        Assert.Equal(["Sort direction must be asc or desc."], messages);
    }

    [Theory]
    [InlineData("sortBy")]
    [InlineData("sortDir")]
    public async Task Empty_sort_value_is_rejected(string parameter) =>
        await AssertBadRequestAsync(await _client.GetAsync($"{ListPath}?{parameter}="), parameter);

    [Fact]
    public async Task Sort_errors_are_returned_together()
    {
        var response = await _client.GetAsync($"{ListPath}?sortBy=bad&sortDir=bad");

        await AssertBadRequestAsync(response, "sortBy");
        await AssertBadRequestAsync(response, "sortDir");
    }

    [Theory]
    [InlineData(ListPath, "Bad")]
    [InlineData(ListPath, "planning")]
    [InlineData(ListPath, "ONSITE")]
    [InlineData(ListPath, "1")]
    [InlineData(ListPath, "99")]
    [InlineData(ListPath, "")]
    [InlineData(SummaryPath, "Bad")]
    [InlineData(SummaryPath, "planning")]
    [InlineData(SummaryPath, "ONSITE")]
    [InlineData(SummaryPath, "1")]
    [InlineData(SummaryPath, "99")]
    [InlineData(SummaryPath, "")]
    public async Task Anything_but_an_exact_status_name_is_rejected(string path, string status)
    {
        var messages = await AssertBadRequestAsync(await _client.GetAsync($"{path}?status={status}"), "status");

        Assert.Equal([StatusMessage], messages);
    }

    [Theory]
    [InlineData(ListPath)]
    [InlineData(SummaryPath)]
    public async Task One_bad_value_rejects_the_whole_filter(string path)
    {
        var messages = await AssertBadRequestAsync(await _client.GetAsync(path + "?status=Planning&status=Bad"), "status");

        Assert.Equal([StatusMessage], messages);
    }

    [Theory]
    [InlineData(ListPath)]
    [InlineData(SummaryPath)]
    public async Task Several_bad_values_report_the_message_once(string path)
    {
        var messages = await AssertBadRequestAsync(await _client.GetAsync(path + "?status=Bad&status=Worse"), "status");

        Assert.Equal([StatusMessage], messages);
    }

    [Fact]
    public async Task Summary_route_is_not_treated_as_an_id_and_lists_every_status_in_pipeline_order()
    {
        var response = await _client.GetAsync(SummaryPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var statuses = json.RootElement.GetProperty("byStatus").EnumerateArray()
            .Select(row => row.GetProperty("status").GetString());

        Assert.Equal(["Appraisal", "Planning", "OnSite", "Complete"], statuses);
    }
}
