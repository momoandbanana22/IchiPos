using IchiPos.CommandLine;
using Xunit;

namespace IchiPos.Tests.CommandLine;

public class CommandLineParserTests
{
    [Fact]
    public void 正常系_文字列のみを指定()
    {
        // Arrange
        var args = new[] { "hello" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Content);
        Assert.Null(result.ImagePath);
    }

    [Fact]
    public void 正常系_文字列と画像パスを指定()
    {
        // Arrange
        var args = new[] { "hello", "--image-path", "C:\\images" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Content);
        Assert.Equal("C:\\images", result.ImagePath);
    }

    [Fact]
    public void 異常系_content未指定()
    {
        // Arrange
        var args = Array.Empty<string>();
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_contentが複数指定()
    {
        // Arrange
        var args = new[] { "hello", "world" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_未定義のオプション()
    {
        // Arrange
        var args = new[] { "hello", "--unknown-option" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_contentが複数指定された場合のエラーメッセージはcontentに言及する()
    {
        // Arrange
        // 仕様F-003: "--" なし第2引数は「contentが複数指定」エラー。
        // 「未定義のオプション」ではなく content に関するメッセージを返すべき。
        var args = new[] { "hello", "world" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("content", result.ErrorMessage);
    }

    [Fact]
    public void 異常系_imagePathのみ指定()
    {
        // Arrange
        var args = new[] { "--image-path", "C:\\images" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_imagePathの後にフォルダパス未指定()
    {
        // Arrange
        var args = new[] { "hello", "--image-path" };
        var parser = new CommandLineParser();

        // Act
        var result = parser.Parse(args);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
