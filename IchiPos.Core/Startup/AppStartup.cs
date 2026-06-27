using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Output;

namespace IchiPos.Startup;

public class AppStartup
{
    private readonly IConfigLoader _configLoader;
    private readonly IIchiPosApplication _app;
    private readonly IOutputWriter _outputWriter;

    public AppStartup(IConfigLoader configLoader, IIchiPosApplication app, IOutputWriter outputWriter)
    {
        _configLoader = configLoader;
        _app = app;
        _outputWriter = outputWriter;
    }

    public async Task<int> RunAsync(string[] args, string baseDirectory)
    {
        var configResult = _configLoader.Load(baseDirectory);
        if (!configResult.IsSuccess)
        {
            _outputWriter.WriteError($"設定エラー: {configResult.ErrorMessage}");
            return 1;
        }

        return await _app.RunAsync(args, configResult.Config!);
    }
}
