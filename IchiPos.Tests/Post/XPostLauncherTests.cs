using IchiPos.Config;
using IchiPos.Post;
using Moq;
using Xunit;

namespace IchiPos.Tests.Post;

public class XPostLauncherTests
{
    [Fact]
    public async Task 正常系_X投稿画面を起動()
    {
        // Arrange
        var content = "テスト投稿";
        var config = new AppConfig
        {
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet"
            }
        };
        
        var mockBrowserLauncher = new Mock<IBrowserLauncher>();
        mockBrowserLauncher.Setup(x => x.OpenAsync(It.IsAny<string>()))
            .ReturnsAsync(BrowserLaunchResult.Success());
        
        var launcher = new XPostLauncher(mockBrowserLauncher.Object);

        // Act
        var result = await launcher.LaunchAsync(content, config);

        // Assert
        Assert.True(result.IsSuccess);
        mockBrowserLauncher.Verify(x => x.OpenAsync(
            It.Is<string>(url => url.StartsWith("https://twitter.com/intent/tweet?text="))), Times.Once);
    }

    [Fact]
    public async Task 正常系_特殊文字を含む投稿テキスト()
    {
        // Arrange
        var content = "テスト投稿 #hashtag @mention";
        var config = new AppConfig
        {
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet"
            }
        };
        
        var mockBrowserLauncher = new Mock<IBrowserLauncher>();
        mockBrowserLauncher.Setup(x => x.OpenAsync(It.IsAny<string>()))
            .ReturnsAsync(BrowserLaunchResult.Success());
        
        var launcher = new XPostLauncher(mockBrowserLauncher.Object);

        // Act
        var result = await launcher.LaunchAsync(content, config);

        // Assert
        Assert.True(result.IsSuccess);
        mockBrowserLauncher.Verify(x => x.OpenAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task 異常系_ブラウザ起動失敗()
    {
        // Arrange
        var content = "テスト投稿";
        var config = new AppConfig
        {
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet"
            }
        };
        
        var mockBrowserLauncher = new Mock<IBrowserLauncher>();
        mockBrowserLauncher.Setup(x => x.OpenAsync(It.IsAny<string>()))
            .ReturnsAsync(BrowserLaunchResult.Failure("ブラウザ起動失敗"));
        
        var launcher = new XPostLauncher(mockBrowserLauncher.Object);

        // Act
        var result = await launcher.LaunchAsync(content, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
