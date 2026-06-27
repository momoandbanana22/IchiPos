using IchiPos.Config;

namespace IchiPos.Post;

public interface IXPostLauncher
{
    Task<XPostLaunchResult> LaunchAsync(string content, AppConfig config);
}

public class XPostLauncher : IXPostLauncher
{
    private readonly IBrowserLauncher _browserLauncher;

    public XPostLauncher(IBrowserLauncher browserLauncher)
    {
        _browserLauncher = browserLauncher;
    }

    public async Task<XPostLaunchResult> LaunchAsync(string content, AppConfig config)
    {
        // URLエンコード（スペースを %20 にするため EscapeDataString を使用）
        var encodedContent = Uri.EscapeDataString(content);
        
        // URL生成
        var url = $"{config.X.PostUrlBase}?text={encodedContent}";
        
        // ブラウザで開く
        var launchResult = await _browserLauncher.OpenAsync(url);
        
        if (!launchResult.IsSuccess)
        {
            return XPostLaunchResult.Failure(launchResult.ErrorMessage!);
        }
        
        return XPostLaunchResult.Success();
    }
}
