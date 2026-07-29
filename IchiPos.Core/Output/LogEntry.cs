namespace IchiPos.Output;

public class LogEntry
{
    public LogSeverity Severity { get; }
    public string Message { get; }

    /// <summary>このログ行を出力した時点の日時(04書 G-006「日時プレフィクス」)。</summary>
    public DateTimeOffset Timestamp { get; }

    public LogEntry(LogSeverity severity, string message, DateTimeOffset timestamp)
    {
        Severity = severity;
        Message = message;
        Timestamp = timestamp;
    }

    /// <summary>P-09 表示用の1行。行頭に日時プレフィクスを付す(04書 G-006「日時プレフィクス」)。</summary>
    public string DisplayText => $"[{Timestamp:yyyy/MM/dd HH:mm:ss}] {Message}";
}
