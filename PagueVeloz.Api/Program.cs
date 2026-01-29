using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

#region More Configuration

builder.Services.AddSingleton<IAccountLockManager, AccountLockManager>();

builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
builder.Services.AddSingleton<IOperationRepository, InMemoryOperationRepository>();


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

builder.Services.AddScoped<DebitUseCase>();

#endregion

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region Seed

using (var scope = app.Services.CreateScope())
{
    var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

    var account = new Account(Guid.NewGuid(), creditLimit: 1000);
    account.Credit(500); // saldo inicial

    if (accountRepo is PagueVeloz.Infrastructure.Repositories.InMemoryAccountRepository repo)
    {
        repo.Add(account);
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
