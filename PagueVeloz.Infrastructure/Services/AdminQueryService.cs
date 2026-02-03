using Microsoft.EntityFrameworkCore;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Infrastructure.Persistence;

namespace PagueVeloz.Infrastructure.Services;

public class AdminQueryService : IAdminQueryService
{
    private readonly PagueVelozDbContext _context;

    public AdminQueryService(PagueVelozDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientDto>> GetAllClientsAsync()
    {
        var clients = await _context.Clients.AsNoTracking().ToListAsync();
        return clients.Select(c => new ClientDto(c.Id, c.Name, c.Email, c.CreatedAt));
    }

    public async Task<IEnumerable<AccountDto>> GetAllAccountsAsync()
    {
        var accounts = await _context.Accounts.AsNoTracking().ToListAsync();
        return accounts.Select(a => new AccountDto(
            a.Id,
            a.ClientId,
            a.Balance,
            a.ReservedBalance,
            a.CreditLimit,
            a.AvailableBalance,
            a.Status,
            a.CreatedAt
        ));
    }

    public async Task<IEnumerable<OperationDto>> GetAllOperationsAsync()
    {
        var operations = await _context.Operations.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
        return operations.Select(o => new OperationDto(
            o.Id,
            o.AccountId,
            o.Type,
            o.Status,
            o.Amount,
            o.CreatedAt,
            o.CompletedAt
        ));
    }
}
