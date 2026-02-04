using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Api.Extensions;
using PagueVeloz.Api.Health;
using System.Reflection;
using System.IO;
using PagueVeloz.Api.Logging;
using PagueVeloz.Api.Metrics;
using PagueVeloz.Api.Middleware;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Persistence;
using PagueVeloz.Infrastructure.Repositories;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/pagueveloz-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PagueVeloz")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to logging
    builder.Host.UseSerilog();

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

    // Register Operation Logger
    builder.Services.AddScoped<IOperationLogger, OperationLogger>();

    // Register Performance Metrics
    builder.Services.AddScoped<IPerformanceMetrics, PerformanceMetrics>();

    // Register Health Check
    builder.Services.AddScoped<ApplicationHealthCheck>();

    // Register Logging Decorator
    builder.Services.AddScoped<LoggingDecorator>();

    // Register Admin query service
    builder.Services.AddScoped<PagueVeloz.Application.Interfaces.IAdminQueryService, PagueVeloz.Infrastructure.Services.AdminQueryService>();

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
    builder.Services.AddSwaggerGen(options =>
    {
        // Include XML comments (from GenerateDocumentationFile) so Swagger shows <summary> and <response> tags
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Add middlewares for logging and error handling
    app.UseMiddleware<HttpLoggingMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

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
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("🌱 SEEDING DATABASE WITH TEST DATA");
            Console.WriteLine(new string('=', 80) + "\n");

            var clients = new List<(string Name, string Email)>
            {
                ("João Silva", "joao.silva@example.com"),
                ("Maria Santos", "maria.santos@example.com"),
                ("Carlos Oliveira", "carlos.oliveira@example.com"),
                ("Ana Costa", "ana.costa@example.com"),
                ("Pedro Martins", "pedro.martins@example.com")
            };

            foreach (var (name, email) in clients)
            {
                var clientId = Guid.NewGuid();
                var client = new PagueVeloz.Infrastructure.Persistence.Entities.ClientEntity
                {
                    Id = clientId,
                    Name = name,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Clients.Add(client);
                await dbContext.SaveChangesAsync();

                // Create 2-3 accounts per client with different scenarios
                var accountCount = Random.Shared.Next(2, 4);
                for (int i = 0; i < accountCount; i++)
                {
                    var account = new Account(clientId, creditLimit: 5000);
                    
                    // Scenario 1: Rich account with balance
                    if (i == 0)
                    {
                        account.Credit(10000);
                    }
                    // Scenario 2: Limited balance, more credit available
                    else if (i == 1)
                    {
                        account.Credit(500);
                    }
                    // Scenario 3: Empty account, uses credit
                    else
                    {
                        account.Credit(100);
                    }

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

                    // Add some operations history for testing
                    var operations = new List<(EOperationType Type, decimal Amount)>
                    {
                        (EOperationType.Credit, 100),
                        (EOperationType.Debit, 50),
                    };

                    foreach (var (opType, amount) in operations)
                    {
                        var operation = new Operation(
                            Guid.NewGuid(),
                            account.Id,
                            opType,
                            amount
                        );
                        operation.Complete();
                        
                        var operationEntity = new PagueVeloz.Infrastructure.Persistence.Entities.OperationEntity
                        {
                            Id = operation.Id,
                            AccountId = operation.AccountId,
                            Type = operation.Type.ToString(),
                            Amount = operation.Amount,
                            Status = operation.Status.ToString(),
                            CreatedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 24))
                        };
                        dbContext.Operations.Add(operationEntity);
                    }
                    await dbContext.SaveChangesAsync();
                }
            }
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

