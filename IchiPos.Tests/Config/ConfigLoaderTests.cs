using IchiPos.Config;
using Xunit;

namespace IchiPos.Tests.Config;

public class ConfigLoaderTests
{
    [Fact]
    public void 正常系_設定ファイルを読み込む()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var configDir = Path.Combine(testDir, "config");
        Directory.CreateDirectory(configDir);
        
        var configContent = @"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
limits:
  misskey_max_length: 5000
  x_max_length: 280
";
        var configPath = Path.Combine(configDir, "config.yaml");
        File.WriteAllText(configPath, configContent);
        
        var loader = new ConfigLoader();

        // Act
        var result = loader.Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Config);
        Assert.Equal("https://misskey.example.com", result.Config.Misskey.InstanceUrl);
        Assert.Equal("test_token", result.Config.Misskey.AccessToken);
        Assert.Equal("public", result.Config.Misskey.Visibility);
        Assert.Equal("https://twitter.com/intent/tweet", result.Config.X.PostUrlBase);
        Assert.Equal(5000, result.Config.Limits.MisskeyMaxLength);
        Assert.Equal(280, result.Config.Limits.XMaxLength);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_設定ファイルが存在しない()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        var loader = new ConfigLoader();

        // Act
        var result = loader.Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_設定ファイルの形式が不正()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var configDir = Path.Combine(testDir, "config");
        Directory.CreateDirectory(configDir);
        
        var configPath = Path.Combine(configDir, "config.yaml");
        File.WriteAllText(configPath, "invalid yaml: [unclosed");
        
        var loader = new ConfigLoader();

        // Act
        var result = loader.Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }
}
