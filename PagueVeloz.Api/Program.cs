using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Persistence;
using PagueVeloz.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

#region More Configuration

// DbContext registration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<PagueVelozDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddSingleton<IAccountLockManager, AccountLockManager>();

// Use EF Core repositories
builder.Services.AddScoped<IAccountRepository, EfCoreAccountRepository>();
builder.Services.AddScoped<IOperationRepository, EfCoreOperationRepository>();

#region RabbitMQ or In-Memory Event Publisher
var rabbitEnabled = builder.Configuration.GetValue<bool>("RabbitMq:Enabled", false);
if (rabbitEnabled)
{
    builder.Services.AddRabbitMq(builder.Configuration);
}
else
{
    builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
}

#endregion

// Register OperationEventPublisher service
builder.Services.AddScoped<OperationEventPublisher>();

// Register all Use Cases
builder.Services.AddScoped<DebitUseCase>();
builder.Services.AddScoped<CreditUseCase>();
builder.Services.AddScoped<ReserveUseCase>();
builder.Services.AddScoped<CaptureUseCase>();
builder.Services.AddScoped<ReleaseUseCase>();
builder.Services.AddScoped<TransferUseCase>();
builder.Services.AddScoped<RevertUseCase>();

#endregion

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region Database Migrations
await app.Services.ApplyMigrationsAsync();
#endregion

#region Seed

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PagueVelozDbContext>();

    // Seed initial data
    if (!dbContext.Clients.Any())
    {
        // Create a client first
        var clientId = Guid.NewGuid();
        var client = new PagueVeloz.Infrastructure.Persistence.Entities.ClientEntity
        {
            Id = clientId,
            Name = "Test Client",
            Email = "test@pagueveloz.com",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Seed ClientId: {clientId}");

        // Create an account for this client
        var account = new Account(clientId, creditLimit: 1000);
        account.Credit(500); // initial balance

        var accountEntity = new PagueVeloz.Infrastructure.Persistence.Entities.AccountEntity
        {
            Id = account.Id,
            ClientId = account.ClientId,
            Balance = account.Balance,
            AvailableBalance = account.AvailableAmount(),
            ReservedBalance = account.ReservedBalance,
            CreditLimit = account.CreditLimit,
            Status = account.Status.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Accounts.Add(accountEntity);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Seed AccountId: {account.Id}");
    }
}

#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
