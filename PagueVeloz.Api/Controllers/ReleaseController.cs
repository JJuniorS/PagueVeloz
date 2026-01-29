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

    [HttpPost]
    public async Task<IActionResult> Release([FromBody] ReleaseRequest request)
    {
        await _useCase.ExecuteAsync(request);
        return Ok(new { message = "Reservation released successfully" });
    }
}
