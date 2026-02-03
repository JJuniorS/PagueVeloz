using System.Diagnostics;

namespace PagueVeloz.Api.Middleware;

/// <summary>
/// Middleware para logging de requisições e respostas HTTP.
/// Rastreia tempo de processamento e status.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks e swagger
        if (context.Request.Path.StartsWithSegments("/health") || 
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "HTTP Request: Method={Method}, Path={Path}, QueryString={QueryString}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString
            );

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP Response: Method={Method}, Path={Path}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "HTTP Request failed: Method={Method}, Path={Path}, ElapsedMs={ElapsedMs}, Error={Error}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                ex.Message
            );

            throw;
        }
    }
}
