using IchiPos.Post;
using Moq;
using Xunit;

namespace IchiPos.Tests.Post;

public class BrowserLauncherTests
{
    [Fact]
    public async Task 正常系_ブラウザを起動する()
    {
        // Arrange
        var mockStarter = new Mock<IProcessStarter>();
        mockStarter.Setup(x => x.Start(It.IsAny<string>())).Returns(true);
        var launcher = new BrowserLauncher(mockStarter.Object);

        // Act
        var result = await launcher.OpenAsync("https://twitter.com/intent/tweet?text=hello");

        // Assert
        Assert.True(result.IsSuccess);
        mockStarter.Verify(x => x.Start("https://twitter.com/intent/tweet?text=hello"), Times.Once);
    }

    [Fact]
    public async Task 異常系_プロセス起動失敗時にエラーを返す()
    {
        // Arrange
        var mockStarter = new Mock<IProcessStarter>();
        mockStarter.Setup(x => x.Start(It.IsAny<string>())).Returns(false);
        var launcher = new BrowserLauncher(mockStarter.Object);

        // Act
        var result = await launcher.OpenAsync("https://twitter.com/intent/tweet?text=hello");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task 異常系_例外発生時にエラーを返す()
    {
        // Arrange
        var mockStarter = new Mock<IProcessStarter>();
        mockStarter.Setup(x => x.Start(It.IsAny<string>()))
            .Throws(new Exception("プロセス起動エラー"));
        var launcher = new BrowserLauncher(mockStarter.Object);

        // Act
        var result = await launcher.OpenAsync("https://twitter.com/intent/tweet?text=hello");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
