using System.Net;
using IchiPos.Config;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Startup;
using Moq;
using Moq.Protected;
using Xunit;

namespace IchiPos.Tests.Startup;

/// <summary>
/// Program.cs と同じ CompositionRoot.BuildApplication で組み立てた「本物のクラス」を、
/// 外部境界（HTTP通信・OSプロセス起動・クリップボード・ファイル削除）だけ偽装して一気通貫で動かす。
/// モックだらけの単体テストでは検出できない「配線ミス」を自動で検出するためのテスト。
/// </summary>
public class CompositionRootTests
{
    private static HttpClient CreateFakeMisskeyHttpClient()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"createdNote\":{\"id\":\"note123\"}}")
            });
        return new HttpClient(mockHandler.Object);
    }

    [Fact]
    public async Task 正常系_MisskeyとXを実クラスの組み合わせで一気通貫実行する()
    {
        // Arrange
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet", Enabled = true },
            Mixi2 = new Mixi2Config { Enabled = false },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockProcessStarter = new Mock<IProcessStarter>();
        mockProcessStarter.Setup(x => x.Start(It.IsAny<string>())).Returns(true);

        var app = CompositionRoot.BuildApplication(
            CreateFakeMisskeyHttpClient(),
            mockProcessStarter.Object,
            new Mixi2ApiClient(),
            new OutputWriter(),
            Mock.Of<IClipboardService>(),
            Mock.Of<IImageCleanupService>());

        // Act
        var result = await app.RunAsync(new[] { "テスト投稿" }, config);

        // Assert
        Assert.Equal(0, result);
        mockProcessStarter.Verify(x => x.Start(It.Is<string>(url => url.StartsWith("https://twitter.com/intent/tweet"))), Times.Once);
    }

    [Fact]
    public async Task 正常系_MIXI2とXが無効なデフォルト設定でもMisskeyには投稿できる()
    {
        // Arrange
        // config.yaml.example のデフォルト状態（mixi2・xともに無効）を模した設定。
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Mixi2 = new Mixi2Config(),
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockProcessStarter = new Mock<IProcessStarter>();

        var app = CompositionRoot.BuildApplication(
            CreateFakeMisskeyHttpClient(),
            mockProcessStarter.Object,
            new Mixi2ApiClient(),
            new OutputWriter(),
            Mock.Of<IClipboardService>(),
            Mock.Of<IImageCleanupService>());

        // Act
        var result = await app.RunAsync(new[] { "テスト投稿" }, config);

        // Assert
        Assert.Equal(0, result);
        mockProcessStarter.Verify(x => x.Start(It.IsAny<string>()), Times.Never);
    }
}
