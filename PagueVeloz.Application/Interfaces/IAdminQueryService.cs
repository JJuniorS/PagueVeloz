using PagueVeloz.Application.DTOs;

namespace PagueVeloz.Application.Interfaces;

public interface IAdminQueryService
{
    Task<IEnumerable<ClientDto>> GetAllClientsAsync();
    Task<IEnumerable<AccountDto>> GetAllAccountsAsync();
    Task<IEnumerable<OperationDto>> GetAllOperationsAsync();
}
