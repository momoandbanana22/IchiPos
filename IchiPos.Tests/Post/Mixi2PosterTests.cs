using IchiPos.Config;
using IchiPos.Post;
using Moq;
using Xunit;

namespace IchiPos.Tests.Post;

public class Mixi2PosterTests
{
    private static AppConfig CreateConfig(bool enabled = true) => new()
    {
        Mixi2 = new Mixi2Config
        {
            Enabled = enabled,
            ClientId = "client_id",
            ClientSecret = "client_secret",
            AccessToken = "mixi2_token"
        }
    };

    private static Mixi2Poster CreatePoster(Mock<IMixi2ApiClient> client) =>
        new(client.Object, delay: _ => Task.CompletedTask);

    [Fact]
    public async Task 正常系_テキストのみ投稿()
    {
        // Arrange
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.CreatePostAsync("テスト投稿", It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Success("post_id_1"));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string>(), config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("post_id_1", result.PostId);
        mockClient.Verify(x => x.CreatePostAsync("テスト投稿", It.Is<List<string>>(ids => ids.Count == 0), config), Times.Once);
        mockClient.Verify(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_画像付き投稿()
    {
        // Arrange
        var config = CreateConfig();
        var imagePaths = new List<string> { "image1.png", "image2.jpg" };
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.SetupSequence(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media_1"))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media_2"));
        mockClient.Setup(x => x.GetMediaStatusAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.Completed));
        mockClient.Setup(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Success("post_id_1"));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", imagePaths, config);

        // Assert
        Assert.True(result.IsSuccess);
        mockClient.Verify(x => x.UploadMediaAsync(It.IsAny<string>(), config), Times.Exactly(2));
        mockClient.Verify(x => x.CreatePostAsync(
            "テスト投稿",
            It.Is<List<string>>(ids => ids.Count == 2 && ids.Contains("media_1") && ids.Contains("media_2")),
            config), Times.Once);
    }

    [Fact]
    public async Task 正常系_画像が5枚以上ある場合は4枚までしか添付しない()
    {
        // Arrange
        var config = CreateConfig();
        var imagePaths = new List<string> { "1.png", "2.png", "3.png", "4.png", "5.png" };
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media"));
        mockClient.Setup(x => x.GetMediaStatusAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.Completed));
        mockClient.Setup(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Success("post_id_1"));

        var poster = CreatePoster(mockClient);

        // Act
        await poster.PostAsync("テスト投稿", imagePaths, config);

        // Assert
        mockClient.Verify(x => x.UploadMediaAsync(It.IsAny<string>(), config), Times.Exactly(4));
    }

    [Fact]
    public async Task 正常系_MIXI2が無効な場合はスキップする()
    {
        // Arrange
        var config = CreateConfig(enabled: false);
        var mockClient = new Mock<IMixi2ApiClient>();
        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string>(), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsSkipped);
        mockClient.Verify(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_画像処理が完了するまでポーリングする()
    {
        // Arrange
        // 一定間隔でポーリングし、完了(Completed)を確認するまで待つ(第9.4節)。
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media_1"));
        mockClient.SetupSequence(x => x.GetMediaStatusAsync("media_1", config))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.UploadPending))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.Processing))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.Completed));
        mockClient.Setup(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Success("post_id_1"));

        var delayCallCount = 0;
        var poster = new Mixi2Poster(mockClient.Object, delay: _ => { delayCallCount++; return Task.CompletedTask; });

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string> { "image1.png" }, config);

        // Assert
        Assert.True(result.IsSuccess);
        mockClient.Verify(x => x.GetMediaStatusAsync("media_1", config), Times.Exactly(3));
        Assert.Equal(2, delayCallCount);
    }

    [Fact]
    public async Task 異常系_画像アップロード失敗()
    {
        // Arrange
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Failure("アップロード失敗"));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string> { "image1.png" }, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsSkipped);
        Assert.NotNull(result.ErrorMessage);
        mockClient.Verify(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像処理状況取得失敗()
    {
        // Arrange
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media_1"));
        mockClient.Setup(x => x.GetMediaStatusAsync("media_1", config))
            .ReturnsAsync(Mixi2MediaStatusResult.Failure("状況取得失敗"));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string> { "image1.png" }, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        mockClient.Verify(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像処理が失敗ステータスを返す()
    {
        // Arrange
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), config))
            .ReturnsAsync(Mixi2MediaUploadResult.Success("media_1"));
        mockClient.Setup(x => x.GetMediaStatusAsync("media_1", config))
            .ReturnsAsync(Mixi2MediaStatusResult.Success(Mixi2MediaStatus.Failed));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string> { "image1.png" }, config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        mockClient.Verify(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_投稿作成失敗()
    {
        // Arrange
        var config = CreateConfig();
        var mockClient = new Mock<IMixi2ApiClient>();
        mockClient.Setup(x => x.CreatePostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Failure("投稿作成失敗"));

        var poster = CreatePoster(mockClient);

        // Act
        var result = await poster.PostAsync("テスト投稿", new List<string>(), config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
