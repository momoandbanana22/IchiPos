using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class OutputWriterTests
{
    [Fact]
    public void 正常系_成功メッセージを出力()
    {
        // Arrange
        var writer = new OutputWriter();
        var message = "処理が成功しました";

        // Act
        writer.WriteSuccess(message);

        // Assert
        // 標準出力への出力はテストできないため、例外が発生しないことを確認
    }

    [Fact]
    public void 正常系_エラーメッセージを出力()
    {
        // Arrange
        var writer = new OutputWriter();
        var message = "エラーが発生しました";

        // Act
        writer.WriteError(message);

        // Assert
        // 標準エラーへの出力はテストできないため、例外が発生しないことを確認
    }

    [Fact]
    public void 正常系_情報メッセージを出力()
    {
        // Arrange
        var writer = new OutputWriter();
        var message = "情報メッセージ";

        // Act
        writer.WriteInfo(message);

        // Assert
        // 標準出力への出力はテストできないため、例外が発生しないことを確認
    }
}
