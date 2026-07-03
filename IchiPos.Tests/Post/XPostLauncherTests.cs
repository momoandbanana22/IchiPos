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
                PostUrlBase = "https://twitter.com/intent/tweet",
                Enabled = true
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
        // # は URL の fragment 区切り文字のため、エンコードされないと X サーバーに届かない。
        // @ も未エンコードだとユーザー情報として解釈される場合がある。
        var content = "テスト投稿 #hashtag @mention";
        var config = new AppConfig
        {
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet",
                Enabled = true
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
            It.Is<string>(url =>
                url.Contains("%23") &&    // # → %23
                url.Contains("%40") &&    // @ → %40
                !url.Contains("#") &&
                !url.Contains("@"))),
            Times.Once);
    }

    [Fact]
    public async Task 正常系_スペースはパーセント20にエンコードされる()
    {
        // Arrange
        // WebUtility.UrlEncode はスペースを "+" にするが、
        // X intent URL では "%20" が正しい。"+" はそのまま表示される。
        var content = "テスト 投稿";
        var config = new AppConfig
        {
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet", Enabled = true }
        };

        var mockBrowserLauncher = new Mock<IBrowserLauncher>();
        mockBrowserLauncher.Setup(x => x.OpenAsync(It.IsAny<string>()))
            .ReturnsAsync(BrowserLaunchResult.Success());

        var launcher = new XPostLauncher(mockBrowserLauncher.Object);

        // Act
        await launcher.LaunchAsync(content, config);

        // Assert
        mockBrowserLauncher.Verify(x => x.OpenAsync(
            It.Is<string>(url => url.Contains("%20") && !url.Contains("+"))), Times.Once);
    }

    [Fact]
    public async Task 正常系_Xが無効な場合はスキップする()
    {
        // Arrange
        var content = "テスト投稿";
        var config = new AppConfig
        {
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet",
                Enabled = false
            }
        };

        var mockBrowserLauncher = new Mock<IBrowserLauncher>();
        var launcher = new XPostLauncher(mockBrowserLauncher.Object);

        // Act
        var result = await launcher.LaunchAsync(content, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsSkipped);
        mockBrowserLauncher.Verify(x => x.OpenAsync(It.IsAny<string>()), Times.Never);
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
                PostUrlBase = "https://twitter.com/intent/tweet",
                Enabled = true
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
