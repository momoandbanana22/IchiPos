namespace IchiPos.Post;

public interface IMisskeyHttpClient
{
    Task<MisskeyUploadResult> UploadImageAsync(string instanceUrl, string accessToken, string imagePath);
    Task<MisskeyPostResult> PostNoteAsync(string instanceUrl, string accessToken, string visibility, string text, List<string> fileIds);
}
