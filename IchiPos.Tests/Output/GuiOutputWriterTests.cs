using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class GuiOutputWriterTests
{
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
}
