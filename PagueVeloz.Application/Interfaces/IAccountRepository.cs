using PagueVeloz.Core.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId);
    Task UpdateAsync(Account account);
}
