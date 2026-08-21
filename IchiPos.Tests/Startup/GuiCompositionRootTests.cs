using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using IchiPos.Config;
using IchiPos.Gui;
using IchiPos.Startup;
using IchiPos.Tests.Gui;
using Moq;
using Xunit;

namespace IchiPos.Tests.Startup;

/// <summary>
/// GuiCompositionRootの配線を検証する自動テスト。Program.cs/GuiEntryPointと同じ組み立てを、
/// 実際にウィンドウを表示せず(手動exe起動の代替として)検証する。
/// MIXI2投稿対応PR(#4)で導入されたCompositionRoot検証テストと同じ狙い。
///
/// WPFのXAML(Window)を生成するため、他のXAML生成テストと同時に初回初期化が走らないよう
/// WPF UI コレクションで直列化する(#93。<see cref="Gui.WpfUiCollection"/> 参照)。
/// </summary>
[Collection(WpfUiCollection.Name)]
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
        var (error, window) = RunOnSta(() => GuiCompositionRoot.BuildMainWindow(config, Mock.Of<IConfigReloader>(), Mock.Of<IThemeApplier>()));

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
            (MainWindowViewModel)GuiCompositionRoot.BuildMainWindow(config, Mock.Of<IConfigReloader>(), Mock.Of<IThemeApplier>()).DataContext);

        // Assert
        Assert.Null(error);
        Assert.NotNull(viewModel);
        Assert.Equal(string.Empty, viewModel!.Content);
    }

    [Fact]
    public void 正常系_文字数表示がconfigのLimitsから算出される()
    {
        // 04書 G-002 第5節: Misskey/Xのうち短い方(この設定ではXMaxLength=280)を上限として表示する。
        // Arrange
        var config = ValidConfig();

        // Act
        var (error, viewModel) = RunOnSta(() =>
            (MainWindowViewModel)GuiCompositionRoot.BuildMainWindow(config, Mock.Of<IConfigReloader>(), Mock.Of<IThemeApplier>()).DataContext);

        // Assert
        Assert.Null(error);
        Assert.Equal("0 / 280 文字", viewModel!.CharacterCountDisplay);
    }

    // ──────────────────────────────────────────────────────────────────
    // 定型文タブの実描画(04書 G-016 第3節、issue #111)
    // XAMLのバインディング・IsSplitによる表示切替は単体テストでは検証できないため、
    // 実際にウィンドウをレイアウトして「実際に表示されるTextBlock」を取り出して検証する。
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 定型文タブ_分割は両本文をラベル付きで_同一は本文のみを表示する_issue111()
    {
        var config = ValidConfig();
        // 1件目: Misskey・X 同一本文（文字列相当）。2件目: 別本文（マップ相当）。
        config.Templates = new List<TemplateEntry>
        {
            new("おはよう", "おはよう"),
            new(":ohayou:", "おはよう(X版)"),
        };

        var (error, visibleTexts) = RunOnSta<List<string>>(() =>
        {
            var window = GuiCompositionRoot.BuildMainWindow(config, Mock.Of<IConfigReloader>(), Mock.Of<IThemeApplier>());

            // Windowを表示せずにレイアウトする（ヘッドレス環境でも描画ツリーを生成できる）。
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(560, 760));
            root.Arrange(new Rect(0, 0, 560, 760));
            root.UpdateLayout();

            // 「定型文」タブ（2番目）を選択して再レイアウトし、その内容を実体化させる。
            var tabControl = FindDescendant<TabControl>(root);
            tabControl!.SelectedIndex = 1;
            root.UpdateLayout();

            var acc = new List<string>();
            CollectEffectivelyVisibleTextBlocks(root, acc);
            return acc;
        });

        Assert.Null(error);
        Assert.NotNull(visibleTexts);
        var dump = "[" + string.Join(" | ", visibleTexts!) + "]";

        // 同一本文の定型文は、ラベルなしで本文を1つだけ表示する。
        Assert.True(visibleTexts!.Contains("おはよう"), dump);
        // 分割した定型文は、Misskey本文・X本文をそれぞれラベル付きで表示する（Runバインディングが解決している証拠）。
        Assert.True(visibleTexts!.Contains("Misskey: :ohayou:"), dump);
        Assert.True(visibleTexts!.Contains("X: おはよう(X版)"), dump);
        // 分割時のラベルなし単一表示（Misskey本文のみ）は Collapsed で、表示されない。
        Assert.False(visibleTexts!.Contains(":ohayou:"), dump);
    }

    /// <summary>
    /// 可視ツリーを走査し、Collapsed/Hidden の要素配下を除いた「実際に表示される」TextBlock のテキストを集める。
    /// TextBlock.Text はインライン(Run)で構成した内容を返さないため、TextRange で実テキストを取り出す。
    /// </summary>
    private static void CollectEffectivelyVisibleTextBlocks(DependencyObject node, List<string> acc)
    {
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is UIElement ue && ue.Visibility != Visibility.Visible)
                continue; // Collapsed/Hidden の要素とその配下は「表示されない」ため除外する。

            if (child is TextBlock tb)
            {
                var text = new TextRange(tb.ContentStart, tb.ContentEnd).Text;
                if (!string.IsNullOrWhiteSpace(text)) acc.Add(text);
            }

            CollectEffectivelyVisibleTextBlocks(child, acc);
        }
    }

    private static T? FindDescendant<T>(DependencyObject node) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T match) return match;
            var found = FindDescendant<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
