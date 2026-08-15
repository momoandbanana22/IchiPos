using IchiPos.Config;

namespace IchiPos.Post;

public interface IMisskeyPoster
{
    /// <summary>
    /// Misskey に投稿する。<paramref name="isSensitive"/> が true の場合、添付画像にセンシティブ
    /// （閲覧注意）フラグを設定する（issue #107、02書 F-007）。画像がない場合は影響しない。
    /// </summary>
    Task<MisskeyPostResult> PostAsync(string content, List<string> imagePaths, AppConfig config, bool isSensitive);
}

public class MisskeyPoster : IMisskeyPoster
{
    private readonly IMisskeyHttpClient _httpClient;

    public MisskeyPoster(IMisskeyHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MisskeyPostResult> PostAsync(string content, List<string> imagePaths, AppConfig config, bool isSensitive)
    {
        var fileIds = new List<string>();

        // 画像がある場合はアップロード
        foreach (var imagePath in imagePaths)
        {
            var uploadResult = await _httpClient.UploadImageAsync(
                config.Misskey.InstanceUrl,
                config.Misskey.AccessToken,
                imagePath,
                isSensitive);

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
            content,
            fileIds);

        return postResult;
    }
}
