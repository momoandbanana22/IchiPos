using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class LogEntryTests
{
    [Fact]
    public void 正常系_DisplayTextは日時プレフィクス付きの本文を返す()
    {
        var entry = new LogEntry(LogSeverity.Info, "Misskey投稿成功: note123",
            new DateTimeOffset(2026, 7, 29, 23, 45, 1, TimeSpan.Zero));

        Assert.Equal("[2026/07/29 23:45:01] Misskey投稿成功: note123", entry.DisplayText);
    }

    [Fact]
    public void 正常系_1桁の月日時分秒はゼロ埋めされる()
    {
        var entry = new LogEntry(LogSeverity.Info, "x",
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

        Assert.Equal("[2026/01/02 03:04:05] x", entry.DisplayText);
    }
}
