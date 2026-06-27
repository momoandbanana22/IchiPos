namespace IchiPos.Post;

public class BrowserLauncher : IBrowserLauncher
{
    private readonly IProcessStarter _processStarter;

    public BrowserLauncher(IProcessStarter processStarter)
    {
        _processStarter = processStarter;
    }

    public Task<BrowserLaunchResult> OpenAsync(string url)
    {
        try
        {
            var success = _processStarter.Start(url);
            return Task.FromResult(success
                ? BrowserLaunchResult.Success()
                : BrowserLaunchResult.Failure("ブラウザの起動に失敗しました"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BrowserLaunchResult.Failure($"ブラウザの起動に失敗しました: {ex.Message}"));
        }
    }
}
