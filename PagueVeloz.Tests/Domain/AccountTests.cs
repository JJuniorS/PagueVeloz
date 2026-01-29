using PagueVeloz.Core.Entities;
using Xunit;

namespace PagueVeloz.Tests.Domain;

public class AccountTests
{
    [Fact]
    public void Debit_Should_Use_Balance_And_CreditLimit()
    {
        var account = new Account(Guid.NewGuid(), creditLimit: 500);
        account.Credit(200);

        account.Debit(600);

        Assert.Equal(-400, account.Balance);
    }

    [Fact]
    public void Debit_Should_Throw_When_Insufficient_Funds()
    {
        var account = new Account(Guid.NewGuid(), creditLimit: 100);
        account.Credit(50);

        Assert.Throws<InvalidOperationException>(() =>
            account.Debit(200));
    }
}
