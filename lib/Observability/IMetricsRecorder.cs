namespace Observability;

public interface IMetricsRecorder
{
    TimedOperation Time(string operationName);
    void Record(string operationName, TimeSpan duration);
}
