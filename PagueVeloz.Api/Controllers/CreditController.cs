using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/credit")]
public class CreditController : ControllerBase
{
    private readonly CreditUseCase _useCase;

    public CreditController(CreditUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Processa um crédito na conta do cliente.
    /// </summary>
    /// <param name="request">Dados do crédito</param>
    /// <response code="200">Crédito processado com sucesso</response>
    [HttpPost]
    public async Task<IActionResult> Credit([FromBody] CreditRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Credit processed successfully" });
    }
}
