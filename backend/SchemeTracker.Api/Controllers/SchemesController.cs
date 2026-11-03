using Microsoft.AspNetCore.Mvc;
using SchemeTracker.Api.Interfaces;

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
}
