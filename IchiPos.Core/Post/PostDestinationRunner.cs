using IchiPos.Config;

namespace IchiPos.Post;

public interface IPostDestinationRunner
{
    Task<SubDestinationsResult> RunAsync(string content, List<string> imagePaths, AppConfig config);
}

public class PostDestinationRunner : IPostDestinationRunner
{
    private readonly IMixi2Poster _mixi2Poster;
    private readonly IXPostLauncher _xPostLauncher;

    public PostDestinationRunner(IMixi2Poster mixi2Poster, IXPostLauncher xPostLauncher)
    {
        _mixi2Poster = mixi2Poster;
        _xPostLauncher = xPostLauncher;
    }

    public async Task<SubDestinationsResult> RunAsync(string content, List<string> imagePaths, AppConfig config)
    {
        // サブ投稿先は登録順（MIXI2 → X）に、互いに独立して実行する。
        // 有効/無効の判定は各サブ投稿先自身が行う（起動条件）。
        // 1つのサブ投稿先が失敗またはスキップしても、後続のサブ投稿先の実行を継続する。
        var mixi2Result = await _mixi2Poster.PostAsync(content, imagePaths, config);
        var xResult = await _xPostLauncher.LaunchAsync(content, config);

        return new SubDestinationsResult(mixi2Result, xResult);
    }
}
