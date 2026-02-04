using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Api.Controllers;

[ApiController]
[Route("api/start")]
public class StartController : ControllerBase
{
    private readonly IAdminQueryService _adminService;

    public StartController(IAdminQueryService adminService)
    {
        _adminService = adminService;
    }


    /// <summary>
    /// Retorna todos os clientes cadastrados (somente para administração).
    /// </summary>
    /// <response code="200">Lista de clientes</response>
    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _adminService.GetAllClientsAsync();
        return Ok(clients);
    }

    /// <summary>
    /// Retorna todas as contas existentes (somente para administração).
    /// </summary>
    /// <response code="200">Lista de contas</response>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _adminService.GetAllAccountsAsync();
        return Ok(accounts);
    }

    /// <summary>
    /// Retorna todas as operações registradas (somente para administração).
    /// </summary>
    /// <response code="200">Lista de operações</response>
    [HttpGet("operations")]
    public async Task<IActionResult> GetOperations()
    {
        var ops = await _adminService.GetAllOperationsAsync();
        return Ok(ops);
    }
}
