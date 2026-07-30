using IchiPos.Config;

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
        var mainWindow = GuiCompositionRoot.BuildMainWindow(configResult.Config!, configReloader);
        var application = new System.Windows.Application();
        return application.Run(mainWindow);
    }
}
