using System.Diagnostics;
using System.Text;

namespace PagueVeloz.Api.Middleware;

/// <summary>
/// Middleware para logging de requisições e respostas HTTP.
/// Captura timing, status code e detalhes para observabilidade.
/// </summary>
public class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpLoggingMiddleware> _logger;

    public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging para Swagger, health checks e arquivos estáticos
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/.well-known") ||
            context.Request.Path.StartsWithSegments("/favicon"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var elapsed = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "HTTP Request: {Method} {Path} - Status: {StatusCode} - {ElapsedMs}ms",
                method, path, statusCode, elapsed
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "HTTP Request failed: {Method} {Path} - {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}
