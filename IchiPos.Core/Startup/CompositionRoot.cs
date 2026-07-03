using IchiPos.Application;
using IchiPos.CommandLine;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Validation;

namespace IchiPos.Startup;

/// <summary>
/// アプリケーションのオブジェクトグラフを組み立てる。
/// 外部境界（HTTP通信・OSプロセス起動・クリップボード・ファイル削除）のみを引数として受け取り、
/// それ以外の内部クラスはすべてここで実クラスを組み立てる。
/// Program.cs と、実クラスを組み合わせたテスト（外部境界のみ偽装）の両方から使われる。
/// </summary>
public static class CompositionRoot
{
    public static IIchiPosApplication BuildApplication(
        HttpClient misskeyHttpClient,
        IProcessStarter processStarter,
        IMixi2ApiClient mixi2ApiClient,
        IOutputWriter outputWriter,
        IClipboardService clipboardService,
        IImageCleanupService imageCleanupService)
    {
        var browserLauncher = new BrowserLauncher(processStarter);
        var mixi2Poster = new Mixi2Poster(mixi2ApiClient);
        var xPostLauncher = new XPostLauncher(browserLauncher);
        var postDestinationRunner = new PostDestinationRunner(mixi2Poster, xPostLauncher);

        return new IchiPosApplication(
            new CommandLineParser(),
            new ContentResolver(new TextFileReader(), new DatePlaceholderReplacer(TimeProvider.System)),
            new ImageFolderReader(),
            new ImageValidator(),
            new PrePostValidator(),
            new MisskeyPoster(new MisskeyHttpClient(misskeyHttpClient)),
            postDestinationRunner,
            outputWriter,
            clipboardService,
            imageCleanupService);
    }
}
