using System;

namespace PagueVeloz.Application.DTOs;

public record ClientDto(Guid Id, string Name, string Email, DateTime CreatedAt);
