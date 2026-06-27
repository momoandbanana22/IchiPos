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
    public async Task 正常系_投稿テキスト取得後に通知を出力する()
    {
        // Arrange
        // F-010 §13.3: 正常時に「投稿テキストを取得したこと」を出力すること。
        // 現在は WriteInfo が一度も呼ばれていない。
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));

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
        mockPrePostValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        mockMisskeyPoster.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));

        var mockXPostLauncher = new Mock<IXPostLauncher>();
        mockXPostLauncher.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(XPostLaunchResult.Success());

        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContentResolver.Object,
            mockImageFolderReader.Object, mockImageValidator.Object,
            mockPrePostValidator.Object, mockMisskeyPoster.Object,
            mockXPostLauncher.Object, mockOutputWriter.Object);

        // Act
        await app.RunAsync(args, config);

        // Assert
        mockOutputWriter.Verify(
            x => x.WriteInfo(It.Is<string>(s => s.Contains("投稿テキスト"))),
            Times.Once);
    }

    [Fact]
    public async Task 正常系_添付画像数を出力する()
    {
        // Arrange
        // F-010 §13.3: 正常時に「添付画像数」を出力すること。
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello"))
            .ReturnsAsync(ContentResolveResult.Success("hello"));

        var mockImageFolderReader = new Mock<IImageFolderReader>();
        mockImageFolderReader.Setup(x => x.Read(@"C:\images"))
            .Returns(ImageFolderReadResult.Success(new List<string> { "a.png", "b.png" }));

        var validPaths = new List<string> { @"C:\images\a.png", @"C:\images\b.png" };
        var mockImageValidator = new Mock<IImageValidator>();
        mockImageValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(validPaths));

        var mockPrePostValidator = new Mock<IPrePostValidator>();
        mockPrePostValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        mockMisskeyPoster.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));

        var mockXPostLauncher = new Mock<IXPostLauncher>();
        mockXPostLauncher.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(XPostLaunchResult.Success());

        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContentResolver.Object,
            mockImageFolderReader.Object, mockImageValidator.Object,
            mockPrePostValidator.Object, mockMisskeyPoster.Object,
            mockXPostLauncher.Object, mockOutputWriter.Object);

        // Act
        await app.RunAsync(args, config);

        // Assert
        mockOutputWriter.Verify(
            x => x.WriteInfo(It.Is<string>(s => s.Contains("添付画像") && s.Contains("2"))),
            Times.Once);
    }

    [Fact]
    public async Task 異常系_投稿テキスト取得失敗時はMisskey投稿を開始しない()
    {
        // Arrange
        var args = new[] { "hello.txt" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello.txt", null));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello.txt"))
            .ReturnsAsync(ContentResolveResult.Failure("ファイルが読めません"));

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object,
            mockContentResolver.Object,
            Mock.Of<IImageFolderReader>(),
            Mock.Of<IImageValidator>(),
            Mock.Of<IPrePostValidator>(),
            mockMisskeyPoster.Object,
            Mock.Of<IXPostLauncher>(),
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(1, result);
        mockOutputWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskeyPoster.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像フォルダ読み込み失敗時はMisskey投稿を開始しない()
    {
        // Arrange
        var args = new[] { "hello", "--image-path", "C:\\images" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello"))
            .ReturnsAsync(ContentResolveResult.Success("hello"));

        var mockImageFolderReader = new Mock<IImageFolderReader>();
        mockImageFolderReader.Setup(x => x.Read(@"C:\images"))
            .Returns(ImageFolderReadResult.Failure("フォルダが存在しません"));

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object,
            mockContentResolver.Object,
            mockImageFolderReader.Object,
            Mock.Of<IImageValidator>(),
            Mock.Of<IPrePostValidator>(),
            mockMisskeyPoster.Object,
            Mock.Of<IXPostLauncher>(),
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(1, result);
        mockOutputWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskeyPoster.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像バリデーション失敗時はMisskey投稿を開始しない()
    {
        // Arrange
        var args = new[] { "hello", "--image-path", "C:\\images" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));

        var mockContentResolver = new Mock<IContentResolver>();
        mockContentResolver.Setup(x => x.ResolveAsync("hello"))
            .ReturnsAsync(ContentResolveResult.Success("hello"));

        var mockImageFolderReader = new Mock<IImageFolderReader>();
        mockImageFolderReader.Setup(x => x.Read(@"C:\images"))
            .Returns(ImageFolderReadResult.Success(new List<string> { "broken.png" }));

        var mockImageValidator = new Mock<IImageValidator>();
        mockImageValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Failure("画像が読み込めません"));

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object,
            mockContentResolver.Object,
            mockImageFolderReader.Object,
            mockImageValidator.Object,
            Mock.Of<IPrePostValidator>(),
            mockMisskeyPoster.Object,
            Mock.Of<IXPostLauncher>(),
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(1, result);
        mockOutputWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskeyPoster.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_投稿前チェック失敗時はMisskey投稿を開始しない()
    {
        // Arrange
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));

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
        mockPrePostValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Failure("テキストが長すぎます"));

        var mockMisskeyPoster = new Mock<IMisskeyPoster>();
        var mockOutputWriter = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object,
            mockContentResolver.Object,
            mockImageFolderReader.Object,
            mockImageValidator.Object,
            mockPrePostValidator.Object,
            mockMisskeyPoster.Object,
            Mock.Of<IXPostLauncher>(),
            mockOutputWriter.Object);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(1, result);
        mockOutputWriter.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskeyPoster.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
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
