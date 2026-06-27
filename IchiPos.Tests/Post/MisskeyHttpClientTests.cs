using IchiPos.Post;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace IchiPos.Tests.Post;

public class MisskeyHttpClientTests
{
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string responseJson)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson)
            });
        return new HttpClient(mockHandler.Object);
    }

    private static (HttpClient, List<Uri>) CreateCapturingHttpClient(HttpStatusCode statusCode, string responseJson)
    {
        var capturedUris = new List<Uri>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.RequestUri != null) capturedUris.Add(req.RequestUri);
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson)
            });
        return (new HttpClient(mockHandler.Object), capturedUris);
    }

    // ──────────────────────────────────────────────
    // PostNoteAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task 正常系_InstanceUrl末尾スラッシュありでも正しいURLにノートを作成する()
    {
        // Arrange
        // config.yaml の instance_url に末尾スラッシュが付いていると
        // "{instanceUrl}/api/notes/create" が "https://example.com//api/notes/create" になってしまう。
        var (httpClient, capturedUris) = CreateCapturingHttpClient(
            HttpStatusCode.OK, "{\"createdNote\":{\"id\":\"note123\"}}");
        var client = new MisskeyHttpClient(httpClient);

        // Act
        await client.PostNoteAsync(
            "https://misskey.example.com/",  // 末尾スラッシュ付き
            "token", "public", "テスト", new List<string>());

        // Assert
        Assert.Single(capturedUris);
        Assert.Equal("https://misskey.example.com/api/notes/create", capturedUris[0].ToString());
    }

    [Fact]
    public async Task 正常系_テキストのみのノートを作成する()
    {
        // Arrange
        var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            "{\"createdNote\":{\"id\":\"note123\"}}");
        var client = new MisskeyHttpClient(httpClient);

        // Act
        var result = await client.PostNoteAsync(
            "https://misskey.example.com",
            "token",
            "public",
            "テスト投稿",
            new List<string>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("note123", result.NoteId);
    }

    [Fact]
    public async Task 正常系_ノート作成時に投稿テキストがリクエストに含まれる()
    {
        // Arrange
        string? capturedBody = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"createdNote\":{\"id\":\"note123\"}}")
            });
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new MisskeyHttpClient(httpClient);

        // Act
        await client.PostNoteAsync(
            "https://misskey.example.com",
            "token",
            "public",
            "テスト投稿",
            new List<string>());

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("テスト投稿", capturedBody);
    }

    [Fact]
    public async Task 異常系_ノート作成HTTPエラー()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "{}");
        var client = new MisskeyHttpClient(httpClient);

        // Act
        var result = await client.PostNoteAsync(
            "https://misskey.example.com",
            "token",
            "public",
            "テスト投稿",
            new List<string>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task 正常系_HTTP200でcreatedNoteキーがない場合も成功とみなす()
    {
        // Arrange
        // HTTP 200 は投稿成功の権威的なシグナル。
        // レスポンスボディに createdNote がなくても成功として扱わないと
        // 実際には投稿済みなのにユーザーにエラーを表示してしまう。
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "{}");
        var client = new MisskeyHttpClient(httpClient);

        // Act
        var result = await client.PostNoteAsync(
            "https://misskey.example.com",
            "token",
            "public",
            "テスト投稿",
            new List<string>());

        // Assert
        Assert.True(result.IsSuccess);
    }

    // ──────────────────────────────────────────────
    // UploadImageAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task 正常系_画像をアップロードする()
    {
        // Arrange
        var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            "{\"id\":\"file123\"}");
        var client = new MisskeyHttpClient(httpClient);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        try
        {
            // Act
            var result = await client.UploadImageAsync(
                "https://misskey.example.com",
                "token",
                tempFile);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("file123", result.FileId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task 異常系_画像アップロードHTTPエラー()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.Unauthorized, "{}");
        var client = new MisskeyHttpClient(httpClient);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[] { 0x00 });
        try
        {
            // Act
            var result = await client.UploadImageAsync(
                "https://misskey.example.com",
                "token",
                tempFile);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task 異常系_画像アップロードAPI応答にIDが含まれない()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "{}");
        var client = new MisskeyHttpClient(httpClient);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[] { 0x00 });
        try
        {
            // Act
            var result = await client.UploadImageAsync(
                "https://misskey.example.com",
                "token",
                tempFile);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task 正常系_InstanceUrl末尾スラッシュありでも正しいURLに画像をアップロードする()
    {
        // Arrange
        var (httpClient, capturedUris) = CreateCapturingHttpClient(
            HttpStatusCode.OK, "{\"id\":\"file123\"}");
        var client = new MisskeyHttpClient(httpClient);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        try
        {
            // Act
            await client.UploadImageAsync(
                "https://misskey.example.com/",  // 末尾スラッシュ付き
                "token",
                tempFile);

            // Assert
            Assert.Single(capturedUris);
            Assert.Equal("https://misskey.example.com/api/drive/files/create", capturedUris[0].ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
