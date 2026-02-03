using System;

namespace PagueVeloz.Application.DTOs;

public record AccountDto(Guid Id, Guid ClientId, decimal Balance, decimal ReservedBalance, decimal CreditLimit, decimal AvailableAmount, string Status, DateTime CreatedAt);
