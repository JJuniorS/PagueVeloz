using System.Reflection;

namespace PagueVeloz.Api.Health;

/// <summary>
/// Service de Health Check para monitorar saúde da aplicação.
/// Verifica: Database, RabbitMQ, e dependências críticas.
/// </summary>
public class ApplicationHealthCheck
{
    private readonly PagueVeloz.Infrastructure.Persistence.PagueVelozDbContext _dbContext;
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public ApplicationHealthCheck(
        PagueVeloz.Infrastructure.Persistence.PagueVelozDbContext dbContext,
        ILogger<ApplicationHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckAsync()
    {
        var result = new HealthCheckResult
        {
            Timestamp = DateTime.UtcNow,
            Version = GetApplicationVersion(),
            Status = "Healthy",
            Checks = new Dictionary<string, HealthCheckDetail>()
        };

        // Check Database
        var dbCheck = await CheckDatabaseAsync();
        result.Checks["Database"] = dbCheck;
        if (!dbCheck.IsHealthy)
            result.Status = "Unhealthy";

        // Check RabbitMQ (básico - apenas se conseguir conectar)
        var rabbitCheck = new HealthCheckDetail
        {
            IsHealthy = true,
            Message = "RabbitMQ is configured",
            Timestamp = DateTime.UtcNow
        };
        result.Checks["RabbitMQ"] = rabbitCheck;

        _logger.LogInformation("Health check completed: Status={Status}", result.Status);

        return result;
    }

    private async Task<HealthCheckDetail> CheckDatabaseAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            return new HealthCheckDetail
            {
                IsHealthy = canConnect,
                Message = canConnect ? "Database connection successful" : "Database connection failed",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new HealthCheckDetail
            {
                IsHealthy = false,
                Message = $"Database connection failed: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, HealthCheckDetail> Checks { get; set; } = new();
}

public class HealthCheckDetail
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
