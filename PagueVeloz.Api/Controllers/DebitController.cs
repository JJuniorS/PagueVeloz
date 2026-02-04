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

        /// <summary>
        /// Realiza um débito na conta do cliente.
        /// </summary>
        /// <remarks>
        /// OperationId deve ser um GUID unico para cada requisição para garantir idempotência.
        /// </remarks>
        /// <param name="request">Dados do débito</param>
        /// <response code="200">Débito processado com sucesso</response>
        [HttpPost]
        public async Task<IActionResult> Debit([FromBody] DebitRequest request)
        {
            await _useCase.ExecuteAsync(request);
            return Ok(new { message = "Debit processed" });
        }
    }
}
