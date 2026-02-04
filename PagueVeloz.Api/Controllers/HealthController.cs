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


    /// <summary>
    /// Retorna o estado de saúde geral da aplicação, incluindo checks dependentes.
    /// </summary>
    /// <response code="200">Aplicação saudável</response>
    /// <response code="503">Aplicação com problemas</response>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var result = await _healthCheck.CheckAsync();
        
        var statusCode = result.Status == "Healthy" 
            ? StatusCodes.Status200OK 
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, result);
    }

    /// <summary>
    /// Verifica se a aplicação está pronta para receber tráfego (readiness).
    /// </summary>
    /// <response code="200">Pronto</response>
    /// <response code="503">Não pronto</response>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        var result = await _healthCheck.CheckAsync();
        return result.Status == "Healthy" ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Indica se a aplicação está viva (liveness).
    /// </summary>
    /// <response code="200">Viva</response>
    [HttpGet("alive")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

