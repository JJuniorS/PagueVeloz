using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/transfer")]
public class TransferController : ControllerBase
{
    private readonly TransferUseCase _useCase;

    public TransferController(TransferUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Transfer processed successfully" });
    }
}
