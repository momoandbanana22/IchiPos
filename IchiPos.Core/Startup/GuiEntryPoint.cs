using IchiPos.Config;

namespace IchiPos.Startup;

/// <summary>GUIモードの起動処理(04書 G-001・G-009)。</summary>
public static class GuiEntryPoint
{
    public static int Run(string baseDirectory)
    {
        var configResult = new ConfigLoader().Load(baseDirectory);
        if (!configResult.IsSuccess)
        {
            System.Windows.MessageBox.Show(
                $"設定エラー: {configResult.ErrorMessage}",
                "IchiPos",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return 1;
        }

        var mainWindow = GuiCompositionRoot.BuildMainWindow(configResult.Config!);
        var application = new System.Windows.Application();
        return application.Run(mainWindow);
    }
}
