using Microsoft.Extensions.Logging;

namespace Observability;

public class ConsoleMetricsRecorder : IMetricsRecorder
{
    private readonly ILogger<ConsoleMetricsRecorder> _logger;

    public ConsoleMetricsRecorder(ILogger<ConsoleMetricsRecorder> logger)
    {
        _logger = logger;
    }

    public TimedOperation Time(string operationName)
    {
        return new TimedOperation(this, operationName);
    }

    // TODO: In a real implementation, this would record to a metrics system instead of logging.
    public void Record(string operationName, TimeSpan duration)
    {
        _logger.LogInformation("[metrics] {Operation} completed in {ElapsedMs:F2}ms",
            operationName, duration.TotalMilliseconds);
    }
}
