using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/reserve")]
public class ReserveController : ControllerBase
{
    private readonly ReserveUseCase _useCase;

    public ReserveController(ReserveUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Reserva um valor na conta do cliente, bloqueando-o para futura captura.
    /// </summary>
    /// <param name="request">Dados da reserva</param>
    /// <response code="200">Valor reservado com sucesso</response>
    [HttpPost]
    public async Task<IActionResult> Reserve([FromBody] ReserveRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Amount reserved successfully" });
    }
}
