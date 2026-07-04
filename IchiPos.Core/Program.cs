using IchiPos.Application;
using IchiPos.CommandLine;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Startup;
using IchiPos.Validation;

// 引数なし起動はGUIモード、引数あり起動はCLIモードとする(04書 4.1節)。
if (new LaunchModeSelector().Determine(args) == LaunchMode.Gui)
{
    var exitCode = 0;
    var guiThread = new Thread(() => exitCode = GuiEntryPoint.Run(AppContext.BaseDirectory));
    guiThread.SetApartmentState(ApartmentState.STA);
    guiThread.Start();
    guiThread.Join();
    return exitCode;
}

// 実行ファイルはGUIサブシステムのため、CLIモードでは起動直後に
// 呼び出し元のコンソールへアタッチする(09.2節)。
ConsoleAttachment.Ensure(new Win32ConsoleAttacher());

var httpClient = new HttpClient();
var misskeyHttpClient = new MisskeyHttpClient(httpClient);
var processStarter = new SystemProcessStarter();
var browserLauncher = new BrowserLauncher(processStarter);

var outputWriter = new OutputWriter();
var app = new IchiPosApplication(
    new CommandLineParser(),
    new ContentResolver(new TextFileReader(), new DatePlaceholderReplacer(TimeProvider.System)),
    new DatePlaceholderReplacer(TimeProvider.System),
    new ImageFolderReader(),
    new ImageValidator(),
    new PrePostValidator(),
    new MisskeyPoster(misskeyHttpClient),
    new XPostLauncher(browserLauncher),
    outputWriter,
    new WindowsClipboardService(),
    new ImageCleanupService(new ConsoleUserPrompt(), outputWriter));

var startup = new AppStartup(new ConfigLoader(), app, new OutputWriter());
return await startup.RunAsync(args, AppContext.BaseDirectory);
