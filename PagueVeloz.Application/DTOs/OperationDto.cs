using System;

namespace PagueVeloz.Application.DTOs;

public record OperationDto(Guid Id, Guid AccountId, string Type, string Status, decimal Amount, DateTime CreatedAt, DateTime? CompletedAt);
