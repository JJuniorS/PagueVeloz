using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Api.Health;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ApplicationHealthCheck _healthCheck;

    public HealthController(ApplicationHealthCheck healthCheck)
    {
        _healthCheck = healthCheck;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var result = await _healthCheck.CheckAsync();
        
        var statusCode = result.Status == "Healthy" 
            ? StatusCodes.Status200OK 
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, result);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        var result = await _healthCheck.CheckAsync();
        return result.Status == "Healthy" ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    [HttpGet("alive")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

