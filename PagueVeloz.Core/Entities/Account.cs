using PagueVeloz.Core.Enums;

namespace PagueVeloz.Core.Entities
{
    public class Account
    {
        public Guid Id { get; private set; }
        public Guid ClientId { get; private set; }
        public decimal Balance { get; private set; }
        public decimal ReservedBalance { get; private set; }
        public decimal CreditLimit { get; private set; }

        public EAccountStatus Status { get; private set; }

        protected Account() { }

        public Account(Guid clientId, decimal creditLimit)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            CreditLimit = creditLimit;
            Balance = 0;
            ReservedBalance = 0;
            Status = EAccountStatus.Active;
        }

        public decimal AvailableAmount()
            => Balance + CreditLimit - ReservedBalance;

        public void Credit(decimal amount)
        {
            AccountIsActive();
            
            if (amount <= 0)
                throw new InvalidOperationException("Credit amount must be greater than zero.");
            
            Balance += amount;
        }

        public void Debit(decimal amount)
        {
            AccountIsActive();
            
            if (amount <= 0)
                throw new InvalidOperationException("Debit amount must be greater than zero.");

            if (AvailableAmount() < amount)
                throw new InvalidOperationException("Insufficient funds.");

            Balance -= amount;
        }

        public void Reserve(decimal amount)
        {
            AccountIsActive();
            
            if (amount <= 0)
                throw new InvalidOperationException("Reserve amount must be greater than zero.");

            if (AvailableAmount() < amount)
                throw new InvalidOperationException("Insufficient funds to reserve.");

            ReservedBalance += amount;
        }

        public void Capture(decimal amount)
        {
            AccountIsActive();
            
            if (amount <= 0)
                throw new InvalidOperationException("Capture amount must be greater than zero.");

            if (ReservedBalance < amount)
                throw new InvalidOperationException("Insufficient reserved balance.");

            ReservedBalance -= amount;
            Balance -= amount;
        }

        public void Release(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Release amount must be greater than zero.");
            
            if (ReservedBalance < amount)
                throw new InvalidOperationException("Insufficient reserved balance.");

            ReservedBalance -= amount;
        }

        private void AccountIsActive()
        {
            if (Status != EAccountStatus.Active)
                throw new InvalidOperationException("Account is not active.");
        }
    }
}
