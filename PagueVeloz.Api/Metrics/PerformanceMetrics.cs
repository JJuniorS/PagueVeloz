using System.Diagnostics;

namespace PagueVeloz.Api.Metrics;

/// <summary>
/// Service para capturar e registrar métricas de performance.
/// </summary>
public interface IPerformanceMetrics
{
    IDisposable StartOperation(string operationName);
    void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null);
}

public class PerformanceMetrics : IPerformanceMetrics
{
    private readonly ILogger<PerformanceMetrics> _logger;

    public PerformanceMetrics(ILogger<PerformanceMetrics> logger)
    {
        _logger = logger;
    }

    public IDisposable StartOperation(string operationName)
    {
        return new OperationTimer(operationName, _logger);
    }

    public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var tagString = tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "";
        _logger.LogInformation(
            "Metric recorded: Name={MetricName}, Value={Value}, Tags={Tags}",
            metricName, value, tagString
        );
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(string operationName, ILogger logger)
        {
            _operationName = operationName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation(
                "Operation completed: Name={OperationName}, ElapsedMs={ElapsedMs}",
                _operationName, _stopwatch.ElapsedMilliseconds
            );
        }
    }
}
