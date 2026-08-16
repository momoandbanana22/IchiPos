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

    // ──────────────────────────────────────────────────────────────────
    // Misskey visibility の妥当性検証 (issue #100)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 異常系_visibilityが許容値以外の場合はエラーを返す_issue100()
    {
        // 許容外の値をそのまま送ると Misskey が 400 を返し原因が分かりにくい。送信前に弾く。
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: invalid_value
x:
  post_url_base: https://twitter.com/intent/tweet
");
        var result = new ConfigLoader().Load(testDir);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("visibility", result.ErrorMessage!);

        Directory.Delete(testDir, true);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("home")]
    [InlineData("followers")]
    [InlineData("specified")]
    public void 正常系_visibilityの許容値を受け付ける_issue100(string visibility)
    {
        var testDir = CreateConfigDir($@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
  visibility: {visibility}
x:
  post_url_base: https://twitter.com/intent/tweet
");
        var result = new ConfigLoader().Load(testDir);

        Assert.True(result.IsSuccess);
        Assert.Equal(visibility, result.Config!.Misskey.Visibility);

        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_visibility未記載は既定publicで有効_issue100()
    {
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
");
        var result = new ConfigLoader().Load(testDir);

        Assert.True(result.IsSuccess);
        Assert.Equal("public", result.Config!.Misskey.Visibility);

        Directory.Delete(testDir, true);
    }

    // ──────────────────────────────────────────────────────────────────
    // 定型文(04書 G-016 第2節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_定型文を記載順のまま読み込む()
    {
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
templates:
  - ""おはよう""
  - ""おやすみ""
  - ""いってきます""
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "おはよう", "おやすみ", "いってきます" }, result.Config!.Templates);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_定型文が未記載でもエラーにせず0件として読み込む()
    {
        // G-016第2節第1項: templatesは任意設定。未記載でも設定読み込みエラーとしない。
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Config!.Templates);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_空文字や空白のみの定型文は一覧から除外する()
    {
        // G-016第2節第4項: 投稿しても投稿前チェックでエラーになるだけの項目は画面に並べない。
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
templates:
  - ""おはよう""
  - """"
  - ""   ""
  - ""おやすみ""
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "おはよう", "おやすみ" }, result.Config!.Templates);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_重複した定型文は除外しない()
    {
        // G-016第2節第5項: 重複は除外せず、登録順をそのまま表示順とする。
        // Arrange
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
templates:
  - ""おはよう""
  - ""おはよう""
");
        // Act
        var result = new ConfigLoader().Load(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "おはよう", "おはよう" }, result.Config!.Templates);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    // ──────────────────────────────────────────────────────────────────
    // ダークモード切替のtheme(04書 G-019、issue #109)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 異常系_themeが許容値以外の場合はエラーを返す_issue109()
    {
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
theme: blue
");
        var result = new ConfigLoader().Load(testDir);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("theme", result.ErrorMessage!);

        Directory.Delete(testDir, true);
    }

    [Theory]
    [InlineData("system")]
    [InlineData("light")]
    [InlineData("dark")]
    public void 正常系_themeの許容値を受け付ける_issue109(string theme)
    {
        var testDir = CreateConfigDir($@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
theme: {theme}
");
        var result = new ConfigLoader().Load(testDir);

        Assert.True(result.IsSuccess);
        Assert.Equal(theme, result.Config!.Theme);

        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_theme未記載は既定systemで有効_issue109()
    {
        var testDir = CreateConfigDir(@"
misskey:
  instance_url: https://misskey.example.com
  access_token: test_token
x:
  post_url_base: https://twitter.com/intent/tweet
");
        var result = new ConfigLoader().Load(testDir);

        Assert.True(result.IsSuccess);
        Assert.Equal("system", result.Config!.Theme);

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
