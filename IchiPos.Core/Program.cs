using IchiPos.Config;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Startup;

var outputWriter = new OutputWriter();
var app = CompositionRoot.BuildApplication(
    new HttpClient(),
    new SystemProcessStarter(),
    new Mixi2ApiClient(),
    outputWriter,
    new WindowsClipboardService(),
    new ImageCleanupService(new ConsoleUserPrompt(), outputWriter));

var startup = new AppStartup(new ConfigLoader(), app, new OutputWriter());
return await startup.RunAsync(args, AppContext.BaseDirectory);
