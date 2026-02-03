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

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _adminService.GetAllClientsAsync();
        return Ok(clients);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _adminService.GetAllAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("operations")]
    public async Task<IActionResult> GetOperations()
    {
        var ops = await _adminService.GetAllOperationsAsync();
        return Ok(ops);
    }
}
