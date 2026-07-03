using IchiPos.Config;

namespace IchiPos.Post;

public interface IMixi2ApiClient
{
    Task<Mixi2MediaUploadResult> UploadMediaAsync(string imagePath, AppConfig config);
    Task<Mixi2MediaStatusResult> GetMediaStatusAsync(string mediaId, AppConfig config);
    Task<Mixi2PostResult> CreatePostAsync(string text, List<string> mediaIds, AppConfig config);
}
