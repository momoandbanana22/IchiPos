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
/// 画像削除確認(F-011)にあたるGUI向け機能(G-004)は廃止したため、GUIでは常に削除しない
/// (クリップボード貼り付けで追加した実ファイルが意図せず削除される事故を避けるため)。
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

        var imageCleanupService = new ImageCleanupService(
            new PresetUserPrompt(() => false),
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

        var viewModel = new MainWindowViewModel(
            app,
            config,
            textFileReader,
            outputWriter,
            new TempClipboardImageStore(),
            new ImageFolderReader(),
            new DatePlaceholderReplacer(TimeProvider.System),
            new FileLastPostStore(),
            new MessageBoxRepostConfirmation());
        return new MainWindow(viewModel);
    }
}
