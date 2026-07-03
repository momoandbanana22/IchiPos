using IchiPos.Config;
using Xunit;

namespace IchiPos.Tests.Config;

public class ConfigLoaderTests
{
    [Fact]
    public void 正常系_同梱のconfig_yaml_exampleがそのままパースできる()
    {
        // Arrange
        // config/config.yaml.example はユーザーが実際にコピーして使うファイル。
        // スキーマとずれていないかをここで自動検証する（手動確認に頼らない）。
        var exampleContent = File.ReadAllText(GetConfigYamlExamplePath());
        var testDir = CreateConfigDir(exampleContent);

        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Config);
        Assert.False(result.Config.X.Enabled);
        Assert.False(result.Config.Mixi2.Enabled);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    private static string GetConfigYamlExamplePath(
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
    {
        // このファイルは IchiPos.Tests/Config/ConfigLoaderTests.cs にあるため、2階層上がリポジトリルート。
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", ".."));
        return Path.Combine(repoRoot, "config", "config.yaml.example");
    }

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
    public void 正常系_MIXI2とXの有効設定を読み込む()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
  enabled: true
mixi2:
  enabled: true
  client_id: test_client_id
  client_secret: test_client_secret
  access_token: test_mixi2_token
limits:
  mixi2_max_length: 300
");
        var loader = new ConfigLoader();

        // Act
        var result = loader.Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Config);
        Assert.True(result.Config.X.Enabled);
        Assert.True(result.Config.Mixi2.Enabled);
        Assert.Equal("test_client_id", result.Config.Mixi2.ClientId);
        Assert.Equal("test_client_secret", result.Config.Mixi2.ClientSecret);
        Assert.Equal("test_mixi2_token", result.Config.Mixi2.AccessToken);
        Assert.Equal(300, result.Config.Limits.Mixi2MaxLength);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_MIXI2とXの有効設定が未指定の場合はデフォルトで無効()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
");
        var loader = new ConfigLoader();

        // Act
        var result = loader.Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Config);
        Assert.False(result.Config.X.Enabled);
        Assert.False(result.Config.Mixi2.Enabled);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_MIXI2が有効なのにClientIdが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
mixi2:
  enabled: true
  client_id: """"
  client_secret: test_client_secret
  access_token: test_mixi2_token
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_MIXI2が有効なのにClientSecretが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
mixi2:
  enabled: true
  client_id: test_client_id
  client_secret: """"
  access_token: test_mixi2_token
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_MIXI2が有効なのにAccessTokenが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
mixi2:
  enabled: true
  client_id: test_client_id
  client_secret: test_client_secret
  access_token: """"
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_MIXI2が無効な場合はMIXI2の認証情報未設定でもエラーにならない()
    {
        // Arrange
        // MIXI2 はデフォルト無効。無効なユーザーに認証情報の入力を強制しない。
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
mixi2:
  enabled: false
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);

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
    public void 異常系_InstanceUrlが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: """"
  access_token: test_token
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_AccessTokenが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: """"
  visibility: public
x:
  post_url_base: https://twitter.com/intent/tweet
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_PostUrlBaseが未設定の場合はエラーを返す()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: public
x:
  post_url_base: """"
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    private static string CreateConfigDir(string yaml)
    {
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configDir = Path.Combine(testDir, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "config.yaml"), yaml);
        return testDir;
    }

    [Fact]
    public void 異常系_設定ファイルが空の場合はエラーを返す()
    {
        // Arrange
        // YamlDotNet は空 YAML を Deserialize すると null を返す。
        // config! で null 免除するとダウンストリームで NullReferenceException が発生するため、
        // 空ファイルはロード段階でエラーとして扱うべき。
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var configDir = Path.Combine(testDir, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "config.yaml"), "");

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
