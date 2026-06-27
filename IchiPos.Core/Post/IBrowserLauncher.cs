namespace IchiPos.Post;

public interface IBrowserLauncher
{
    Task<BrowserLaunchResult> OpenAsync(string url);
}
