namespace PagueVeloz.Application.Interfaces
{
    public interface IAccountLockManager
    {
        Task<IDisposable> AcquireAsync(Guid accountId);
    }
}
