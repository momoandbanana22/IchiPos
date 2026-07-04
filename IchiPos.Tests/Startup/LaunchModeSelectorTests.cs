using IchiPos.Startup;
using Xunit;

namespace IchiPos.Tests.Startup;

public class LaunchModeSelectorTests
{
    [Fact]
    public void 正常系_引数なしの場合はGuiモード()
    {
        // Arrange
        var args = Array.Empty<string>();
        var selector = new LaunchModeSelector();

        // Act
        var mode = selector.Determine(args);

        // Assert
        Assert.Equal(LaunchMode.Gui, mode);
    }

    [Fact]
    public void 正常系_contentのみ指定した場合はCliモード()
    {
        // Arrange
        var args = new[] { "hello" };
        var selector = new LaunchModeSelector();

        // Act
        var mode = selector.Determine(args);

        // Assert
        Assert.Equal(LaunchMode.Cli, mode);
    }

    [Fact]
    public void 正常系_versionオプションのみでもCliモード()
    {
        // Arrange
        var args = new[] { "--version" };
        var selector = new LaunchModeSelector();

        // Act
        var mode = selector.Determine(args);

        // Assert
        Assert.Equal(LaunchMode.Cli, mode);
    }

    [Fact]
    public void 正常系_contentと画像パスを指定した場合はCliモード()
    {
        // Arrange
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var selector = new LaunchModeSelector();

        // Act
        var mode = selector.Determine(args);

        // Assert
        Assert.Equal(LaunchMode.Cli, mode);
    }
}
