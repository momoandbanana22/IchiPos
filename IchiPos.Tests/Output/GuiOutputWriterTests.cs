using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class GuiOutputWriterTests
{
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 29, 23, 45, 1, TimeSpan.Zero);

    [Fact]
    public void 正常系_成功メッセージをSuccess種別で追加する()
    {
        // Arrange
        var writer = new GuiOutputWriter();

        // Act
        writer.WriteSuccess("Misskey投稿成功: note123");

        // Assert
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(LogSeverity.Success, entry.Severity);
        Assert.Equal("Misskey投稿成功: note123", entry.Message);
    }

    [Fact]
    public void 正常系_エラーメッセージをError種別で追加する()
    {
        // Arrange
        var writer = new GuiOutputWriter();

        // Act
        writer.WriteError("Misskey投稿エラー: 通信に失敗しました");

        // Assert
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal("Misskey投稿エラー: 通信に失敗しました", entry.Message);
    }

    [Fact]
    public void 正常系_情報メッセージをInfo種別で追加する()
    {
        // Arrange
        var writer = new GuiOutputWriter();

        // Act
        writer.WriteInfo("添付画像: 2枚");

        // Assert
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(LogSeverity.Info, entry.Severity);
        Assert.Equal("添付画像: 2枚", entry.Message);
    }

    [Fact]
    public void 正常系_警告メッセージをWarning種別で追加する()
    {
        // Arrange
        var writer = new GuiOutputWriter();

        // Act
        writer.WriteWarning("非対応の画像形式を除外しました: readme.txt");

        // Assert
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(LogSeverity.Warning, entry.Severity);
        Assert.Equal("非対応の画像形式を除外しました: readme.txt", entry.Message);
    }

    [Fact]
    public void 正常系_複数回の出力が時系列で追記される()
    {
        // Arrange
        var writer = new GuiOutputWriter();

        // Act
        writer.WriteInfo("1件目");
        writer.WriteSuccess("2件目");
        writer.WriteError("3件目");

        // Assert
        Assert.Equal(3, writer.Entries.Count);
        Assert.Equal("1件目", writer.Entries[0].Message);
        Assert.Equal("2件目", writer.Entries[1].Message);
        Assert.Equal("3件目", writer.Entries[2].Message);
    }

    [Fact]
    public void 正常系_Clearでログをすべて消去する()
    {
        // Arrange
        var writer = new GuiOutputWriter();
        writer.WriteInfo("消えるべきメッセージ");

        // Act
        writer.Clear();

        // Assert
        Assert.Empty(writer.Entries);
    }

    [Fact]
    public void 正常系_ログ行に出力時点の日時が記録される()
    {
        // Arrange
        var writer = new GuiOutputWriter(new FixedTimeProvider(FixedNow));

        // Act
        writer.WriteSuccess("Misskey投稿成功: note123");

        // Assert
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(FixedNow, entry.Timestamp);
        Assert.Equal("[2026/07/29 23:45:01] Misskey投稿成功: note123", entry.DisplayText);
    }

    [Fact]
    public void 正常系_すべての種別のログ行の先頭に日時が付く()
    {
        // Arrange
        var writer = new GuiOutputWriter(new FixedTimeProvider(FixedNow));

        // Act
        writer.WriteInfo("情報");
        writer.WriteSuccess("成功");
        writer.WriteWarning("警告");
        writer.WriteError("エラー");

        // Assert
        Assert.Equal("[2026/07/29 23:45:01] 情報", writer.Entries[0].DisplayText);
        Assert.Equal("[2026/07/29 23:45:01] 成功", writer.Entries[1].DisplayText);
        Assert.Equal("[2026/07/29 23:45:01] 警告", writer.Entries[2].DisplayText);
        Assert.Equal("[2026/07/29 23:45:01] エラー", writer.Entries[3].DisplayText);
    }
}
