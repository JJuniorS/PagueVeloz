using System.Diagnostics;

namespace PagueVeloz.Api.Middleware;

/// <summary>
/// Middleware para logging estruturado de requisições e respostas HTTP.
/// Registra method, path, status code e tempo de execução.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        _logger.LogInformation(
            "HTTP Request started: Method={Method}, Path={Path}, RemoteIP={RemoteIP}",
            method, path, context.Connection.RemoteIpAddress
        );

        try
        {
            await _next(context);

            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP Response completed: Method={Method}, Path={Path}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "HTTP Request failed: Method={Method}, Path={Path}, ElapsedMs={ElapsedMs}",
                method, path, stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}
