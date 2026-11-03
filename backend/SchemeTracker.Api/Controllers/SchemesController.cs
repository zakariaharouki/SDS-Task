using Microsoft.AspNetCore.Mvc;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Services;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Controllers;

[ApiController]
[Route("api/schemes")]
public class SchemesController : ControllerBase
{
    private readonly ISchemeService _schemeService;

    public SchemesController(ISchemeService schemeService)
    {
        _schemeService = schemeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchemeResponse>>> GetAll(
        [FromQuery] SchemeListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _schemeService.ListAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchemeResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _schemeService.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    [HttpPost]
    public async Task<ActionResult<SchemeResponse>> Create(
        [FromBody] CreateSchemeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _schemeService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SchemeResponse>> Update(
        string id,
        [FromBody] UpdateSchemeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _schemeService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SchemeSummaryResponse>> GetSummary(
        [FromQuery(Name = "status")] SchemeStatus[]? statuses,
        CancellationToken cancellationToken)
    {
        var result = await _schemeService.GetSummaryAsync(
            statuses,
            cancellationToken);

        return Ok(result);
    }
}
