using System.Diagnostics;

namespace Observability;

public sealed class TimedOperation : IDisposable
{
    private readonly IMetricsRecorder _recorder;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public TimedOperation(IMetricsRecorder recorder, string operationName)
    {
        _recorder = recorder;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _recorder.Record(_operationName, _stopwatch.Elapsed);
    }
}
