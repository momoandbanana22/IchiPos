using IchiPos.Config;

namespace IchiPos.Post;

public class MisskeyPoster
{
    private readonly IMisskeyHttpClient _httpClient;

    public MisskeyPoster(IMisskeyHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MisskeyPostResult> PostAsync(string content, List<string> imagePaths, AppConfig config)
    {
        var fileIds = new List<string>();

        // 画像がある場合はアップロード
        foreach (var imagePath in imagePaths)
        {
            var uploadResult = await _httpClient.UploadImageAsync(
                config.Misskey.InstanceUrl,
                config.Misskey.AccessToken,
                imagePath);

            if (!uploadResult.IsSuccess)
            {
                return MisskeyPostResult.Failure(uploadResult.ErrorMessage!);
            }

            fileIds.Add(uploadResult.FileId!);
        }

        // ノート作成
        var postResult = await _httpClient.PostNoteAsync(
            config.Misskey.InstanceUrl,
            config.Misskey.AccessToken,
            config.Misskey.Visibility,
            fileIds);

        return postResult;
    }
}
