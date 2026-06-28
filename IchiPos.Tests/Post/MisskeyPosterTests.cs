using IchiPos.Config;
using IchiPos.Post;
using Moq;
using Xunit;

namespace IchiPos.Tests.Post;

public class MisskeyPosterTests
{
    [Fact]
    public async Task 正常系_テキストのみ投稿()
    {
        // Arrange
        var content = "テスト投稿";
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            }
        };

        var mockHttpClient = new Mock<IMisskeyHttpClient>();
        mockHttpClient.Setup(x => x.PostNoteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>>()))
            .ReturnsAsync(MisskeyPostResult.Success("note_id_123"));

        var poster = new MisskeyPoster(mockHttpClient.Object);

        // Act
        var result = await poster.PostAsync(content, new List<string>(), config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("note_id_123", result.NoteId);
        mockHttpClient.Verify(x => x.PostNoteAsync(
            "https://misskey.example.com",
            "test_token",
            "public",
            "テスト投稿",
            It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task 正常系_画像付き投稿()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string> { "image1.png", "image2.jpg" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            }
        };
        
        var mockHttpClient = new Mock<IMisskeyHttpClient>();
        mockHttpClient.Setup(x => x.UploadImageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(MisskeyUploadResult.Success("file_id_1"));
        
        mockHttpClient.Setup(x => x.PostNoteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>>()))
            .ReturnsAsync(MisskeyPostResult.Success("note_id_123"));

        var poster = new MisskeyPoster(mockHttpClient.Object);

        // Act
        var result = await poster.PostAsync(content, imagePaths, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("note_id_123", result.NoteId);
        mockHttpClient.Verify(x => x.UploadImageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task 正常系_画像付き投稿でfileIdsがPostNoteに正しく渡される()
    {
        // Arrange
        // 画像アップロードで得た FileId が集約され PostNoteAsync に渡されることを検証する。
        // 既存の「画像付き投稿」テストは UploadImageAsync の呼び出し回数しか見ておらず、
        // fileIds が空リストでも通過してしまう。
        var content = "テスト投稿";
        var imagePaths = new List<string> { "image1.png", "image2.jpg" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            }
        };

        var mockHttpClient = new Mock<IMisskeyHttpClient>();
        mockHttpClient.SetupSequence(x => x.UploadImageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(MisskeyUploadResult.Success("file_id_1"))
            .ReturnsAsync(MisskeyUploadResult.Success("file_id_2"));

        mockHttpClient.Setup(x => x.PostNoteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>>()))
            .ReturnsAsync(MisskeyPostResult.Success("note_id_123"));

        var poster = new MisskeyPoster(mockHttpClient.Object);

        // Act
        await poster.PostAsync(content, imagePaths, config);

        // Assert
        mockHttpClient.Verify(x => x.PostNoteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<List<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("file_id_1") &&
                ids.Contains("file_id_2"))),
            Times.Once);
    }

    [Fact]
    public async Task 異常系_画像アップロード失敗()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string> { "image1.png" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            }
        };
        
        var mockHttpClient = new Mock<IMisskeyHttpClient>();
        mockHttpClient.Setup(x => x.UploadImageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(MisskeyUploadResult.Failure("アップロード失敗"));
        
        var poster = new MisskeyPoster(mockHttpClient.Object);

        // Act
        var result = await poster.PostAsync(content, imagePaths, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task 異常系_ノート作成失敗()
    {
        // Arrange
        var content = "テスト投稿";
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            }
        };
        
        var mockHttpClient = new Mock<IMisskeyHttpClient>();
        mockHttpClient.Setup(x => x.PostNoteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>>()))
            .ReturnsAsync(MisskeyPostResult.Failure("ノート作成失敗"));
        
        var poster = new MisskeyPoster(mockHttpClient.Object);

        // Act
        var result = await poster.PostAsync(content, new List<string>(), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
