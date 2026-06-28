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
    // ──────────────────────────────────────────────────────────────────
    // ヘルパー：フル正常系のモックセットを組み立てる
    // ──────────────────────────────────────────────────────────────────
    private static IchiPosApplication BuildApp(
        Mock<ICommandLineParser> parser,
        Mock<IContentResolver> content,
        Mock<IImageFolderReader> folder,
        Mock<IImageValidator> validator,
        Mock<IPrePostValidator> prePost,
        Mock<IMisskeyPoster> misskey,
        Mock<IXPostLauncher> x,
        Mock<IOutputWriter> output,
        Mock<IClipboardService>? clipboard = null,
        Mock<IImageCleanupService>? cleanup = null) =>
        new IchiPosApplication(
            parser.Object, content.Object, folder.Object,
            validator.Object, prePost.Object, misskey.Object,
            x.Object, output.Object,
            clipboard?.Object ?? Mock.Of<IClipboardService>(),
            cleanup?.Object ?? Mock.Of<IImageCleanupService>());

    // ──────────────────────────────────────────────────────────────────
    // 正常系
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 正常系_文字列のみ投稿()
    {
        var args = new[] { "hello" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), 280)).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config)).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config), Times.Once);
        mockX.Verify(x => x.LaunchAsync("hello", config), Times.Once);
    }

    [Fact]
    public async Task 正常系_投稿テキスト取得後に通知を出力する()
    {
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        await app.RunAsync(args, config);

        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("投稿テキスト"))), Times.Once);
    }

    [Fact]
    public async Task 正常系_添付画像数を出力する()
    {
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(new List<string> { "a.png", "b.png" }));
        var validPaths = new List<string> { @"C:\images\a.png", @"C:\images\b.png" };
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(validPaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        await app.RunAsync(args, config);

        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("添付画像") && s.Contains("2"))), Times.Once);
    }

    [Fact]
    public async Task 正常系_X投稿成功後に1枚目の画像をクリップボードにコピーする()
    {
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(new List<string> { "a.png", "b.png" }));
        var validPaths = new List<string> { @"C:\images\a.png", @"C:\images\b.png" };
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(validPaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImage(@"C:\images\a.png"), Times.Once);
    }

    [Fact]
    public async Task 正常系_画像なし投稿後はクリップボードを操作しない()
    {
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_投稿後に画像削除サービスを呼ぶ()
    {
        // Arrange
        // X 投稿成功後に IImageCleanupService.RunAsync が validPaths で呼ばれること。
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(new List<string> { "a.png" }));
        var validPaths = new List<string> { @"C:\images\a.png" };
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(validPaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockCleanup = new Mock<IImageCleanupService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), cleanup: mockCleanup);

        // Act
        await app.RunAsync(args, config);

        // Assert
        mockCleanup.Verify(
            x => x.RunAsync(It.Is<List<string>>(paths => paths.SequenceEqual(validPaths))),
            Times.Once);
    }

    [Fact]
    public async Task 正常系_画像なし投稿後は画像削除サービスを呼ばない()
    {
        // Arrange
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockCleanup = new Mock<IImageCleanupService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), cleanup: mockCleanup);

        // Act
        await app.RunAsync(args, config);

        // Assert
        mockCleanup.Verify(x => x.RunAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_versionオプション指定時にバージョンを出力して終了する()
    {
        var args = new[] { "--version" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.VersionRequest());
        var mockOutput = new Mock<IOutputWriter>();
        var mockMisskey = new Mock<IMisskeyPoster>();

        var app = BuildApp(mockParser, new Mock<IContentResolver>(), new Mock<IImageFolderReader>(),
            new Mock<IImageValidator>(), new Mock<IPrePostValidator>(), mockMisskey,
            new Mock<IXPostLauncher>(), mockOutput);

        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("1.0.1"))), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // 異常系
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 異常系_コマンドライン引数エラー()
    {
        var args = Array.Empty<string>();
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Failure("引数エラー"));
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, Mock.Of<IContentResolver>(), Mock.Of<IImageFolderReader>(),
            Mock.Of<IImageValidator>(), Mock.Of<IPrePostValidator>(), Mock.Of<IMisskeyPoster>(),
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.NotEqual(0, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task 異常系_投稿テキスト取得失敗時はMisskey投稿を開始しない()
    {
        var args = new[] { "hello.txt" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello.txt", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello.txt")).ReturnsAsync(ContentResolveResult.Failure("ファイルが読めません"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, Mock.Of<IImageFolderReader>(),
            Mock.Of<IImageValidator>(), Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像フォルダ読み込み失敗時はMisskey投稿を開始しない()
    {
        var args = new[] { "hello", "--image-path", "C:\\images" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Failure("フォルダが存在しません"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, mockFolder.Object,
            Mock.Of<IImageValidator>(), Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_画像バリデーション失敗時はMisskey投稿を開始しない()
    {
        var args = new[] { "hello", "--image-path", "C:\\images" };
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(new List<string> { "broken.png" }));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Failure("画像が読み込めません"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, mockFolder.Object,
            mockValidator.Object, Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_投稿前チェック失敗時はMisskey投稿を開始しない()
    {
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Failure("テキストが長すぎます"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, mockFolder.Object,
            mockValidator.Object, mockPrePost.Object, mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_Misskey投稿失敗時はX投稿画面を開かない()
    {
        var args = new[] { "hello" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), 280)).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config))
            .ReturnsAsync(MisskeyPostResult.Failure("Misskey投稿失敗"));
        var mockX = new Mock<IXPostLauncher>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, mockFolder.Object,
            mockValidator.Object, mockPrePost.Object, mockMisskey.Object,
            mockX.Object, Mock.Of<IOutputWriter>(),
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.NotEqual(0, result);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }
}
