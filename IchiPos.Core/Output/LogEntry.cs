namespace IchiPos.Output;

public class LogEntry
{
    public LogSeverity Severity { get; }
    public string Message { get; }

    public LogEntry(LogSeverity severity, string message)
    {
        Severity = severity;
        Message = message;
    }
}
