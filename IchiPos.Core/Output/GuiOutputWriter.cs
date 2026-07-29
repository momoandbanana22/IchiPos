using System.Collections.ObjectModel;

namespace IchiPos.Output;

/// <summary>結果出力機能(F-010)のGUI版(04書 G-006)。標準出力・標準エラーの代わりに画面ログへ追記する。</summary>
public class GuiOutputWriter : IOutputWriter
{
    private readonly TimeProvider _timeProvider;

    public GuiOutputWriter() : this(TimeProvider.System) { }

    public GuiOutputWriter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void WriteSuccess(string message) => Add(LogSeverity.Success, message);

    public void WriteError(string message) => Add(LogSeverity.Error, message);

    public void WriteInfo(string message) => Add(LogSeverity.Info, message);

    public void WriteWarning(string message) => Add(LogSeverity.Warning, message);

    public void Clear() => Entries.Clear();

    // 各行に、出力した時点の日時を記録する(04書 G-006「日時プレフィクス」)。
    private void Add(LogSeverity severity, string message)
        => Entries.Add(new LogEntry(severity, message, _timeProvider.GetLocalNow()));
}
