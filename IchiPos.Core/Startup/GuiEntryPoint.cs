using IchiPos.Config;
using IchiPos.Gui;

namespace IchiPos.Startup;

/// <summary>GUIモードの起動処理(04書 G-001・G-009)。</summary>
public static class GuiEntryPoint
{
    public static int Run(string baseDirectory)
    {
        var configLoader = new ConfigLoader();
        var configResult = configLoader.Load(baseDirectory);
        if (!configResult.IsSuccess)
        {
            System.Windows.MessageBox.Show(
                $"設定エラー: {configResult.ErrorMessage}",
                "IchiPos",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return 1;
        }

        // 起動後の再読み込み(04書 G-017)は、起動時と同じ ConfigLoader・同じベースディレクトリで再実行する。
        var configReloader = new ConfigReloader(configLoader, baseDirectory);
        // 起動時・再読み込み時(MainWindowViewModel.ReloadConfig)の双方で同じインスタンスを使う。
        var themeApplier = new WpfThemeApplier();

        // 順序が重要: Application生成 → テーマ適用(04書 G-019第3節第1項) → Window構築 の順にする。
        // ThemeMode(Fluentテーマ)はApplicationレベルのリソースとして適用されるため、Windowと配下の
        // コントロールを構築する前に確定させておかないと、一部コントロール(TextBox・Button・TabControl等)に
        // テーマが行き渡らず、背景だけ暗く中身はライトのままになる(issue #109、実機確認で判明)。
        var application = new System.Windows.Application();
        themeApplier.Apply(configResult.Config!.Theme);
        var mainWindow = GuiCompositionRoot.BuildMainWindow(configResult.Config!, configReloader, themeApplier);
        return application.Run(mainWindow);
    }
}
