using IchiPos.Application;
using IchiPos.CommandLine;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Startup;
using IchiPos.Validation;

var httpClient = new HttpClient();
var misskeyHttpClient = new MisskeyHttpClient(httpClient);
var processStarter = new SystemProcessStarter();
var browserLauncher = new BrowserLauncher(processStarter);
var mixi2Poster = new Mixi2Poster(new Mixi2ApiClient());
var xPostLauncher = new XPostLauncher(browserLauncher);
var postDestinationRunner = new PostDestinationRunner(mixi2Poster, xPostLauncher);

var outputWriter = new OutputWriter();
var app = new IchiPosApplication(
    new CommandLineParser(),
    new ContentResolver(new TextFileReader(), new DatePlaceholderReplacer(TimeProvider.System)),
    new ImageFolderReader(),
    new ImageValidator(),
    new PrePostValidator(),
    new MisskeyPoster(misskeyHttpClient),
    postDestinationRunner,
    outputWriter,
    new WindowsClipboardService(),
    new ImageCleanupService(new ConsoleUserPrompt(), outputWriter));

var startup = new AppStartup(new ConfigLoader(), app, new OutputWriter());
return await startup.RunAsync(args, AppContext.BaseDirectory);
