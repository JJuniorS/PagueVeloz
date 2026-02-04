using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/release")]
public class ReleaseController : ControllerBase
{
    private readonly ReleaseUseCase _useCase;

    public ReleaseController(ReleaseUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Libera uma reserva de fundos previamente realizada.
    /// </summary>
    /// <param name="request">Dados da liberação</param>
    /// <response code="200">Reserva liberada com sucesso</response>
    [HttpPost]
    public async Task<IActionResult> Release([FromBody] ReleaseRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Reservation released successfully" });
    }
}
