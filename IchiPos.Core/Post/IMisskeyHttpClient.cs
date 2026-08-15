namespace IchiPos.Post;

public interface IMisskeyHttpClient
{
    /// <summary>
    /// 画像を Misskey Drive にアップロードする。<paramref name="isSensitive"/> が true の場合、
    /// 画像にセンシティブ（閲覧注意）フラグを設定する（issue #107、02書 F-007）。
    /// </summary>
    Task<MisskeyUploadResult> UploadImageAsync(string instanceUrl, string accessToken, string imagePath, bool isSensitive);
    Task<MisskeyPostResult> PostNoteAsync(string instanceUrl, string accessToken, string visibility, string text, List<string> fileIds);
}
