using IchiPos.Config;
using IchiPos.Gui;
using IchiPos.Startup;
using Xunit;

namespace IchiPos.Tests.Startup;

/// <summary>
/// GuiCompositionRootの配線を検証する自動テスト。Program.cs/GuiEntryPointと同じ組み立てを、
/// 実際にウィンドウを表示せず(手動exe起動の代替として)検証する。
/// MIXI2投稿対応PR(#4)で導入されたCompositionRoot検証テストと同じ狙い。
/// </summary>
public class GuiCompositionRootTests
{
    private static AppConfig ValidConfig() => new AppConfig
    {
        Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
        X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
        Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
    };

    /// <summary>WPFのWindow生成はSTAスレッドを要求するため、専用スレッドで実行する。</summary>
    private static (Exception? Error, T? Result) RunOnSta<T>(Func<T> func) where T : class
    {
        Exception? error = null;
        T? result = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return (error, result);
    }

    [Fact]
    public void 正常系_有効な設定からMainWindowを構築できる()
    {
        // Arrange
        var config = ValidConfig();

        // Act
        var (error, window) = RunOnSta(() => GuiCompositionRoot.BuildMainWindow(config));

        // Assert
        Assert.Null(error);
        Assert.NotNull(window);
    }

    [Fact]
    public void 正常系_MainWindowのDataContextはMainWindowViewModelである()
    {
        // Arrange
        var config = ValidConfig();

        // Act
        // WindowのDataContext(DependencyProperty)は生成スレッドでしか読めないため、STAスレッド内で取り出す。
        var (error, viewModel) = RunOnSta(() =>
            (MainWindowViewModel)GuiCompositionRoot.BuildMainWindow(config).DataContext);

        // Assert
        Assert.Null(error);
        Assert.NotNull(viewModel);
        Assert.Equal(string.Empty, viewModel!.Content);
        Assert.False(viewModel.DeleteImagesAfterPost);
    }

    [Fact]
    public void 正常系_文字数表示がconfigのLimitsから算出される()
    {
        // 04書 G-002 第5節: Misskey/Xのうち短い方(この設定ではXMaxLength=280)を上限として表示する。
        // Arrange
        var config = ValidConfig();

        // Act
        var (error, viewModel) = RunOnSta(() =>
            (MainWindowViewModel)GuiCompositionRoot.BuildMainWindow(config).DataContext);

        // Assert
        Assert.Null(error);
        Assert.Equal("0 / 280 文字", viewModel!.CharacterCountDisplay);
    }
}
