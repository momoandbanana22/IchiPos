using IchiPos.Application;
using IchiPos.CommandLine;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Validation;
using Moq;
using Xunit;

namespace IchiPos.Tests.Application;

public class ApplicationTests
{
    [Fact]
    public async Task 正常系_文字列のみ投稿()
    {
        // Arrange
        var args = new[] { "hello" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            },
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet"
            },
            Limits = new LimitsConfig
            {
                MisskeyMaxLength = 5000,
                XMaxLength = 280
            }
        };

        var mockCommandLineParser = new Mock<ICommandLineParser>();
        mockCommandLineParser.Setup(x => x.Parse(args))
            .Returns(ParseResult.Success("hello", null));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello"))
            .ReturnsAsync(ContentResolveResult.Success("hello"));

        var mockImageFolderReader = new Mock<IImageFolderReader>();
        mockImageFolderReader.Setup(x => x.Read(null))
            .Returns(ImageFolderReadResult.Success(new List<string>()));

        var mockImageValidator = new Mock<IImageValidator>();
        mockImageValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));

        var mockPrePostValidator = new Mock<IPrePostValidator>();
        mockPrePostValidator.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), 280))
            .Returns(PrePostValidationResult.Success());

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        mockMisskeyPoster.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config))
            .ReturnsAsync(MisskeyPostResult.Success("note_id_123"));

        var mockXPostLauncher = new Mock<IXPostLauncher>();
        mockXPostLauncher.Setup(x => x.LaunchAsync("hello", config))
            .ReturnsAsync(XPostLaunchResult.Success());

        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockCommandLineParser.Object,
            mockContentResolver.Object,
            mockImageFolderReader.Object,
            mockImageValidator.Object,
            mockPrePostValidator.Object,
            mockMisskeyPoster.Object,
            mockXPostLauncher.Object,
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(0, result);
        mockMisskeyPoster.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config), Times.Once);
        mockXPostLauncher.Verify(x => x.LaunchAsync("hello", config), Times.Once);
    }

    [Fact]
    public async Task 異常系_コマンドライン引数エラー()
    {
        // Arrange
        var args = Array.Empty<string>();
        var config = new AppConfig();

        var mockCommandLineParser = new Mock<ICommandLineParser>();
        mockCommandLineParser.Setup(x => x.Parse(args))
            .Returns(ParseResult.Failure("引数エラー"));

        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockCommandLineParser.Object,
            Mock.Of<IContentResolver>(),
            Mock.Of<IImageFolderReader>(),
            Mock.Of<IImageValidator>(),
            Mock.Of<IPrePostValidator>(),
            Mock.Of<IMisskeyPoster>(),
            Mock.Of<IXPostLauncher>(),
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.NotEqual(0, result);
        mockOutputWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task 異常系_Misskey投稿失敗時はX投稿画面を開かない()
    {
        // Arrange
        var args = new[] { "hello" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig
            {
                InstanceUrl = "https://misskey.example.com",
                AccessToken = "test_token",
                Visibility = "public"
            },
            X = new XConfig
            {
                PostUrlBase = "https://twitter.com/intent/tweet"
            },
            Limits = new LimitsConfig
            {
                MisskeyMaxLength = 5000,
                XMaxLength = 280
            }
        };

        var mockCommandLineParser = new Mock<ICommandLineParser>();
        mockCommandLineParser.Setup(x => x.Parse(args))
            .Returns(ParseResult.Success("hello", null));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello"))
            .ReturnsAsync(ContentResolveResult.Success("hello"));

        var mockImageFolderReader = new Mock<IImageFolderReader>();
        mockImageFolderReader.Setup(x => x.Read(null))
            .Returns(ImageFolderReadResult.Success(new List<string>()));

        var mockImageValidator = new Mock<IImageValidator>();
        mockImageValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));

        var mockPrePostValidator = new Mock<IPrePostValidator>();
        mockPrePostValidator.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), 280))
            .Returns(PrePostValidationResult.Success());

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        mockMisskeyPoster.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config))
            .ReturnsAsync(MisskeyPostResult.Failure("Misskey投稿失敗"));

        var mockXPostLauncher = new Mock<IXPostLauncher>();

        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockCommandLineParser.Object,
            mockContentResolver.Object,
            mockImageFolderReader.Object,
            mockImageValidator.Object,
            mockPrePostValidator.Object,
            mockMisskeyPoster.Object,
            mockXPostLauncher.Object,
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.NotEqual(0, result);
        mockXPostLauncher.Verify(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }
}
