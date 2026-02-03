using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/capture")]
public class CaptureController : ControllerBase
{
    private readonly CaptureUseCase _useCase;

    public CaptureController(CaptureUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<IActionResult> Capture([FromBody] CaptureRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Capture processed successfully" });
    }
}
