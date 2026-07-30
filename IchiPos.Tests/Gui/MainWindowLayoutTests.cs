using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>
/// GUI レイアウトの回帰テスト(#93)。
/// 結果ログ領域(P-09)が行の蓄積で最大高まで自動拡大しても、「投稿する」ボタン(P-08)が
/// タブの表示領域からはみ出して消えないことを、実際の MainWindow.xaml を計測して検証する。
///
/// バグの構造: 外側 Grid でタブは Height="*"(残り物)・ログは Height="Auto" のため、
/// ログが MaxHeight まで育つと * のタブが押しつぶされ、スクロールしないタブ内容の最下行
/// (投稿するボタン)が枠外へクリップされて消えていた。
/// </summary>
[Collection(WpfUiCollection.Name)]
public class MainWindowLayoutTests
{
    // 既定ウィンドウ(Width=560 / Height=680, 04書「画面イメージ」節)の内容領域に相当するサイズ。
    // 外側 Grid の Margin(12) と枠を差し引いた、行が実際に取り合う領域の目安。
    private const double ContentWidth = 520;
    private const double ContentHeight = 596;

    [Fact]
    public void 投稿ボタンは_ログ満杯かつ画像添付でも_タブ表示領域からはみ出さない()
    {
        var (buttonBottom, tabHeight) = RunOnStaThread(() =>
        {
            var win = LoadWindowFromXaml();
            var root = (FrameworkElement)win.Content;
            win.Content = null; // Window を Show せずに内容だけを計測する(ヘッドレス/CI 安全)。

            var size = new Size(ContentWidth, ContentHeight);
            var rect = new Rect(new Point(0, 0), size);

            // 1回目の Measure/Arrange で可視ツリー(TabControl テンプレート等)を実体化する。
            root.Measure(size);
            root.Arrange(rect);

            // 結果ログを蓄積させ、ListBox を MaxHeight まで張り付かせる(1回投稿後の状態を模擬)。
            var listBox = FindDescendant<ListBox>(root, _ => true)!;
            listBox.ItemsSource = Enumerable.Range(1, 30).Select(i => $"log line {i}").ToList();

            // 添付画像1枚(#93 の再現状況。サムネイル領域は固定高のため高さに影響しないことも兼ねる)。
            var thumbScrollViewer = FindDescendant<ScrollViewer>(root, sv => sv.Height == 88);
            var thumbItems = thumbScrollViewer is null
                ? null
                : FindDescendant<ItemsControl>(thumbScrollViewer, ic => ic is not ListBox);
            if (thumbItems is not null)
            {
                thumbItems.ItemsSource = new[] { new AttachedImage("dummy.png", isTemporary: false) };
            }

            // 追加した項目を反映して再レイアウトする。
            root.Measure(size);
            root.Arrange(rect);
            root.UpdateLayout();

            var tabControl = FindDescendant<TabControl>(root, _ => true)!;
            var postButton = FindDescendant<Button>(root, b => (b.Content as string) == "投稿する")!;

            // 「投稿する」ボタンの下端を TabControl 座標系で求める。
            var toTab = postButton.TransformToAncestor(tabControl);
            var bottom = toTab.Transform(new Point(0, postButton.ActualHeight)).Y;
            return (bottom, tabControl.ActualHeight);
        });

        Assert.True(
            buttonBottom <= tabHeight + 1.0,
            $"投稿ボタンがタブ表示領域からはみ出しています(クリップ)。" +
            $" ボタン下端={buttonBottom:F1} > タブ高={tabHeight:F1}");
    }

    // ── ヘルパ ──────────────────────────────────────────────────────────

    /// <summary>
    /// WPF ビジュアルの生成・計測は STA スレッドを要するため、専用スレッドで実行する。
    /// Dispatcher.Run() のメッセージループを回し、計測後に InvokeShutdown() することで、
    /// レイアウト/レンダリングで生成した WPF 資源(MediaContext 等)を正しく破棄する。これを
    /// 怠ると、他の WPF テスト(例: GuiCompositionRootTests)と同居した際にテストホストの
    /// プロセス終了がブロックされ、ハングと誤検知される。
    /// </summary>
    private static T RunOnStaThread<T>(Func<T> func)
    {
        T result = default!;
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try { result = func(); }
                catch (Exception ex) { captured = ex; }
                finally { dispatcher.InvokeShutdown(); }
            }));
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        if (captured is not null)
        {
            throw new InvalidOperationException("STA スレッドでの計測に失敗しました。", captured);
        }

        return result;
    }

    /// <summary>
    /// 実 XAML(出力へコピーした MainWindow.xaml)を読み込み、コード連携要素(x:Class・イベント・
    /// local 型・Icon)だけ取り除いて XamlReader で実体化する。レイアウト(固定高・余白・構造)は保持する。
    /// </summary>
    private static Window LoadWindowFromXaml()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Gui", "MainWindow.xaml");
        var xaml = File.ReadAllText(path);
        xaml = Regex.Replace(xaml, "x:Class=\"[^\"]*\"", "");
        xaml = Regex.Replace(xaml, "xmlns:local=\"[^\"]*\"", "");
        xaml = Regex.Replace(xaml, "Icon=\"[^\"]*\"", "");
        xaml = Regex.Replace(xaml, "<local:FilePathToThumbnailConverter[^/]*/>", "");
        xaml = Regex.Replace(xaml, ",\\s*Converter=\\{StaticResource FilePathToThumbnailConverter\\}", "");
        // XamlReader は分離コードのイベントハンドラを解決できないため属性ごと除去する。
        xaml = Regex.Replace(xaml, "\\s(PreviewKeyDown|Click|MouseMove|Drop|PreviewMouseLeftButtonDown)=\"[^\"]*\"", "");
        return (Window)XamlReader.Parse(xaml);
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : class
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is T typed && predicate(typed))
            {
                return typed;
            }

            var count = VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                stack.Push(VisualTreeHelper.GetChild(node, i));
            }
        }

        return null;
    }
}
