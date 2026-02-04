using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/revert")]
public class RevertController : ControllerBase
{
    private readonly RevertUseCase _useCase;

    public RevertController(RevertUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Reverte uma operação financeira previamente executada.
    /// </summary>
    /// <param name="request">Dados da reversão</param>
    /// <response code="200">Operação revertida com sucesso</response>
    [HttpPost]
    public async Task<IActionResult> Revert([FromBody] RevertRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Operation reverted successfully" });
    }
}
