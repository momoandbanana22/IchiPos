using IchiPos.Config;
using Moq;
using Xunit;

namespace IchiPos.Tests.Config;

/// <summary>
/// ConfigReloader(04書 G-017)の検証。起動時と同じ ConfigLoader・同じベースディレクトリで
/// 設定を読み直すことを確認する。
/// </summary>
public class ConfigReloaderTests
{
    private static AppConfig ValidConfig() => new AppConfig
    {
        Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
        X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
        Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
    };

    [Fact]
    public void 正常系_起動時と同じベースディレクトリでLoadを呼ぶ()
    {
        var loader = new Mock<IConfigLoader>();
        loader.Setup(x => x.Load(@"C:\base")).Returns(ConfigLoadResult.Success(ValidConfig()));
        var reloader = new ConfigReloader(loader.Object, @"C:\base");

        reloader.Reload();

        loader.Verify(x => x.Load(@"C:\base"), Times.Once);
    }

    [Fact]
    public void 正常系_Loaderの結果をそのまま返す()
    {
        var expected = ConfigLoadResult.Success(ValidConfig());
        var loader = new Mock<IConfigLoader>();
        loader.Setup(x => x.Load(It.IsAny<string>())).Returns(expected);
        var reloader = new ConfigReloader(loader.Object, @"C:\base");

        var result = reloader.Reload();

        Assert.Same(expected, result);
    }

    [Fact]
    public void 異常系_読み込み失敗の結果もそのまま返す()
    {
        var loader = new Mock<IConfigLoader>();
        loader.Setup(x => x.Load(It.IsAny<string>())).Returns(ConfigLoadResult.Failure("設定ファイルが存在しません"));
        var reloader = new ConfigReloader(loader.Object, @"C:\base");

        var result = reloader.Reload();

        Assert.False(result.IsSuccess);
        Assert.Equal("設定ファイルが存在しません", result.ErrorMessage);
    }
}
