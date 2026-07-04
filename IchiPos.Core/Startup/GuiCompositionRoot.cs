using IchiPos.Application;
using IchiPos.CommandLine;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Gui;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Validation;

namespace IchiPos.Startup;

/// <summary>
/// GUIモードの構成(04書 9.1〜9.5節)。CLIと共通の投稿処理層(F-004〜F-011)をGUI向けに組み立てる。
/// 画像削除確認(F-011)は、投稿前チェックボックス(G-004)を参照するPresetUserPromptに置き換える。
/// </summary>
public static class GuiCompositionRoot
{
    public static MainWindow BuildMainWindow(AppConfig config)
    {
        var outputWriter = new GuiOutputWriter();
        var httpClient = new HttpClient();
        var misskeyHttpClient = new MisskeyHttpClient(httpClient);
        var processStarter = new SystemProcessStarter();
        var browserLauncher = new BrowserLauncher(processStarter);
        var textFileReader = new TextFileReader();

        // ImageCleanupServiceはPresetUserPrompt経由でviewModelのチェックボックス状態を参照する。
        // viewModelはIchiPosApplication(と、それに依存するimageCleanupService)より後で構築するため、
        // ラムダのクロージャ経由で後から代入されるviewModelを参照する(実行時にはviewModelは構築済み)。
        MainWindowViewModel? viewModel = null;
        var imageCleanupService = new ImageCleanupService(
            new PresetUserPrompt(() => viewModel!.DeleteImagesAfterPost),
            outputWriter);

        var app = new IchiPosApplication(
            new CommandLineParser(),
            new ContentResolver(textFileReader, new DatePlaceholderReplacer(TimeProvider.System)),
            new DatePlaceholderReplacer(TimeProvider.System),
            new ImageFolderReader(),
            new ImageValidator(),
            new PrePostValidator(),
            new MisskeyPoster(misskeyHttpClient),
            new XPostLauncher(browserLauncher),
            outputWriter,
            new WindowsClipboardService(),
            imageCleanupService);

        viewModel = new MainWindowViewModel(app, config, textFileReader, outputWriter, new TempClipboardImageStore());
        return new MainWindow(viewModel);
    }
}
