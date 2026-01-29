using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Core.Entities
{
    public class Client
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private readonly List<Account> _accounts = new();
        public IReadOnlyCollection<Account> Accounts => _accounts;

        protected Client() { }

        public Client(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public void AddAccount(Account account)
        {
            _accounts.Add(account);
        }
    }
}
