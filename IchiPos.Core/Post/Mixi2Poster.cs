using IchiPos.Config;

namespace IchiPos.Post;

public interface IMixi2Poster
{
    Task<Mixi2PostResult> PostAsync(string content, List<string> imagePaths, AppConfig config);
}

public class Mixi2Poster : IMixi2Poster
{
    private const int MaxImages = 4;
    private const int MaxPollAttempts = 30;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IMixi2ApiClient _client;
    private readonly Func<TimeSpan, Task> _delay;

    public Mixi2Poster(IMixi2ApiClient client, Func<TimeSpan, Task>? delay = null)
    {
        _client = client;
        _delay = delay ?? Task.Delay;
    }

    public async Task<Mixi2PostResult> PostAsync(string content, List<string> imagePaths, AppConfig config)
    {
        if (!config.Mixi2.Enabled)
        {
            return Mixi2PostResult.Skipped();
        }

        // MIXI2 は1投稿あたり最大4枚まで添付できる（第9.3節）。
        var targetImages = imagePaths.Take(MaxImages).ToList();
        var mediaIds = new List<string>();

        foreach (var imagePath in targetImages)
        {
            var uploadResult = await _client.UploadMediaAsync(imagePath, config);
            if (!uploadResult.IsSuccess)
            {
                return Mixi2PostResult.Failure(uploadResult.ErrorMessage!);
            }

            var waitResult = await WaitForMediaCompletionAsync(uploadResult.MediaId!, config);
            if (!waitResult.IsSuccess)
            {
                return Mixi2PostResult.Failure(waitResult.ErrorMessage!);
            }

            mediaIds.Add(uploadResult.MediaId!);
        }

        return await _client.CreatePostAsync(content, mediaIds, config);
    }

    private async Task<(bool IsSuccess, string? ErrorMessage)> WaitForMediaCompletionAsync(string mediaId, AppConfig config)
    {
        for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            var statusResult = await _client.GetMediaStatusAsync(mediaId, config);
            if (!statusResult.IsSuccess)
            {
                return (false, statusResult.ErrorMessage);
            }

            if (statusResult.Status == Mixi2MediaStatus.Completed)
            {
                return (true, null);
            }

            if (statusResult.Status == Mixi2MediaStatus.Failed)
            {
                return (false, "画像の処理に失敗しました");
            }

            await _delay(PollInterval);
        }

        return (false, "画像処理の完了確認がタイムアウトしました");
    }
}
