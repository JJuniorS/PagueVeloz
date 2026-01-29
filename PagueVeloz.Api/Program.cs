using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region More Configuration

builder.Services.AddSingleton<AccountLockManager>();

builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
builder.Services.AddSingleton<IOperationRepository, InMemoryOperationRepository>();
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

builder.Services.AddScoped<DebitUseCase>();

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
