using Api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(IHealthService healthService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(healthService.GetStatus());
}
