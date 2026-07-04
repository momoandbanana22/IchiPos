using System.Collections.ObjectModel;

namespace IchiPos.Output;

/// <summary>結果出力機能(F-010)のGUI版(04書 G-006)。標準出力・標準エラーの代わりに画面ログへ追記する。</summary>
public class GuiOutputWriter : IOutputWriter
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void WriteSuccess(string message) => Entries.Add(new LogEntry(LogSeverity.Success, message));

    public void WriteError(string message) => Entries.Add(new LogEntry(LogSeverity.Error, message));

    public void WriteInfo(string message) => Entries.Add(new LogEntry(LogSeverity.Info, message));

    public void WriteWarning(string message) => Entries.Add(new LogEntry(LogSeverity.Warning, message));

    public void Clear() => Entries.Clear();
}
