using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;

namespace PagueVeloz.Api.Controllers
{
    [ApiController]
    [Route("api/debit")]
    public class DebitController : ControllerBase
    {
        private readonly DebitUseCase _useCase;

        public DebitController(DebitUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        public async Task<IActionResult> Debit([FromBody] DebitRequest request)
        {
            await _useCase.ExecuteAsync(request);
            return Ok(new { message = "Debit processed" });
        }
    }
}
