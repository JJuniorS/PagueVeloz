using System.Collections.Concurrent;

namespace PagueVeloz.Infrastructure.Locks;

public class AccountLockManager
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(Guid accountId)
    {
        var semaphore = _locks.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        return new Releaser(accountId, semaphore);
    }

    private class Releaser : IDisposable
    {
        private readonly Guid _accountId;
        private readonly SemaphoreSlim _semaphore;

        public Releaser(Guid accountId, SemaphoreSlim semaphore)
        {
            _accountId = accountId;
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
