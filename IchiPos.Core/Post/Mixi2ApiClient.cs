using IchiPos.Config;

namespace IchiPos.Post;

/// <summary>
/// MIXI2 API(gRPC, OAuth 2.0)との実通信は未実装のプレースホルダ。
/// 実際のMIXI2開発者登録・認証情報が確定した段階で実装する(Issue #3)。
/// </summary>
public class Mixi2ApiClient : IMixi2ApiClient
{
    private const string NotImplementedMessage = "MIXI2 APIとの通信は未実装です";

    public Task<Mixi2MediaUploadResult> UploadMediaAsync(string imagePath, AppConfig config)
    {
        return Task.FromResult(Mixi2MediaUploadResult.Failure(NotImplementedMessage));
    }

    public Task<Mixi2MediaStatusResult> GetMediaStatusAsync(string mediaId, AppConfig config)
    {
        return Task.FromResult(Mixi2MediaStatusResult.Failure(NotImplementedMessage));
    }

    public Task<Mixi2PostResult> CreatePostAsync(string text, List<string> mediaIds, AppConfig config)
    {
        return Task.FromResult(Mixi2PostResult.Failure(NotImplementedMessage));
    }
}
