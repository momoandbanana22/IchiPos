using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IchiPos.Post;

public class MisskeyHttpClient : IMisskeyHttpClient
{
    private readonly HttpClient _httpClient;

    public MisskeyHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MisskeyUploadResult> UploadImageAsync(string instanceUrl, string accessToken, string imagePath)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(imagePath);
            var fileName = Path.GetFileName(imagePath);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(accessToken), "i");
            form.Add(new ByteArrayContent(fileBytes), "file", fileName);

            var baseUrl = instanceUrl.TrimEnd('/');
            var response = await _httpClient.PostAsync($"{baseUrl}/api/drive/files/create", form);

            if (!response.IsSuccessStatusCode)
            {
                return MisskeyUploadResult.Failure($"画像アップロードに失敗しました: HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetString();
                if (id != null) return MisskeyUploadResult.Success(id);
            }

            return MisskeyUploadResult.Failure("画像アップロード応答にIDが含まれていません");
        }
        catch (Exception ex)
        {
            return MisskeyUploadResult.Failure($"画像アップロードに失敗しました: {ex.Message}");
        }
    }

    public async Task<MisskeyPostResult> PostNoteAsync(
        string instanceUrl,
        string accessToken,
        string visibility,
        string text,
        List<string> fileIds)
    {
        try
        {
            // fileIds が空のとき "fileIds": [] を送ると 400 を返す Misskey インスタンスがあるため、
            // 画像がある場合のみフィールドを含める。
            object body = fileIds.Count > 0
                ? new { i = accessToken, text, visibility, fileIds }
                : (object)new { i = accessToken, text, visibility };

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(body, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = instanceUrl.TrimEnd('/');
            var response = await _httpClient.PostAsync($"{baseUrl}/api/notes/create", content);

            if (!response.IsSuccessStatusCode)
            {
                return MisskeyPostResult.Failure($"ノートの作成に失敗しました: HTTP {(int)response.StatusCode}");
            }

            // HTTP 200 は投稿成功の権威的なシグナル。
            // レスポンスボディの構造に関わらず成功とみなす。
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            string? noteId = null;
            if (doc.RootElement.TryGetProperty("createdNote", out var noteProp) &&
                noteProp.TryGetProperty("id", out var idProp))
            {
                noteId = idProp.GetString();
            }

            return MisskeyPostResult.Success(noteId ?? string.Empty);
        }
        catch (Exception ex)
        {
            return MisskeyPostResult.Failure($"ノートの作成に失敗しました: {ex.Message}");
        }
    }
}
