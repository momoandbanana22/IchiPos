using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class OutputWriterTests
{
    [Fact]
    public void 正常系_成功メッセージを出力()
    {
        // Arrange
        var outWriter = new StringWriter();
        var writer = new OutputWriter(outWriter, TextWriter.Null);

        // Act
        writer.WriteSuccess("処理が成功しました");

        // Assert
        Assert.Contains("処理が成功しました", outWriter.ToString());
    }

    [Fact]
    public void 正常系_エラーメッセージを出力()
    {
        // Arrange
        var errWriter = new StringWriter();
        var writer = new OutputWriter(TextWriter.Null, errWriter);

        // Act
        writer.WriteError("エラーが発生しました");

        // Assert
        Assert.Contains("エラーが発生しました", errWriter.ToString());
    }

    [Fact]
    public void 正常系_情報メッセージを出力()
    {
        // Arrange
        var outWriter = new StringWriter();
        var writer = new OutputWriter(outWriter, TextWriter.Null);

        // Act
        writer.WriteInfo("情報メッセージ");

        // Assert
        Assert.Contains("情報メッセージ", outWriter.ToString());
    }
}
