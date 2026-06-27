using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Output;
using IchiPos.Startup;
using Moq;
using Xunit;

namespace IchiPos.Tests.Startup;

public class StartupTests
{
    private static AppConfig ValidConfig() => new AppConfig
    {
        Misskey = new MisskeyConfig
        {
            InstanceUrl = "https://misskey.example.com",
            AccessToken = "test_token",
            Visibility = "public"
        },
        X = new XConfig
        {
            PostUrlBase = "https://twitter.com/intent/tweet"
        },
        Limits = new LimitsConfig
        {
            MisskeyMaxLength = 5000,
            XMaxLength = 280
        }
    };

    [Fact]
    public async Task 異常系_設定ファイル読み込み失敗時はエラーを出力してコード1を返す()
    {
        // Arrange
        var mockConfigLoader = new Mock<IConfigLoader>();
        mockConfigLoader.Setup(x => x.Load(It.IsAny<string>()))
            .Returns(ConfigLoadResult.Failure("設定ファイルが存在しません"));

        var mockApp = new Mock<IIchiPosApplication>();
        var mockWriter = new Mock<IOutputWriter>();

        var startup = new AppStartup(mockConfigLoader.Object, mockApp.Object, mockWriter.Object);

        // Act
        var result = await startup.RunAsync(new[] { "hello" }, @"C:\base");

        // Assert
        Assert.Equal(1, result);
        mockWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockApp.Verify(x => x.RunAsync(It.IsAny<string[]>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_設定成功時はアプリを実行する()
    {
        // Arrange
        var mockConfigLoader = new Mock<IConfigLoader>();
        mockConfigLoader.Setup(x => x.Load(It.IsAny<string>()))
            .Returns(ConfigLoadResult.Success(ValidConfig()));

        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string[]>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(0);

        var startup = new AppStartup(
            mockConfigLoader.Object,
            mockApp.Object,
            Mock.Of<IOutputWriter>());

        // Act
        var result = await startup.RunAsync(new[] { "hello" }, @"C:\base");

        // Assert
        Assert.Equal(0, result);
        mockApp.Verify(x => x.RunAsync(
            It.Is<string[]>(a => a[0] == "hello"),
            It.IsAny<AppConfig>()), Times.Once);
    }

    [Fact]
    public async Task 正常系_アプリの終了コードがそのまま返される()
    {
        // Arrange
        var mockConfigLoader = new Mock<IConfigLoader>();
        mockConfigLoader.Setup(x => x.Load(It.IsAny<string>()))
            .Returns(ConfigLoadResult.Success(ValidConfig()));

        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string[]>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(1);

        var startup = new AppStartup(
            mockConfigLoader.Object,
            mockApp.Object,
            Mock.Of<IOutputWriter>());

        // Act
        var result = await startup.RunAsync(new[] { "hello" }, @"C:\base");

        // Assert
        Assert.Equal(1, result);
    }
}
