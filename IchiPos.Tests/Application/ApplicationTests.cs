using IchiPos;
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
    // サブ投稿先（MIXI2、X）がともに成功したとみなす既定のランナー結果。
    private static SubDestinationsResult DefaultSubResult() =>
        new(Mixi2PostResult.Skipped(), XPostLaunchResult.Success());

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
        Mock<IPostDestinationRunner> runner,
        Mock<IOutputWriter> output,
        Mock<IClipboardService>? clipboard = null,
        Mock<IImageCleanupService>? cleanup = null) =>
        new IchiPosApplication(
            parser.Object, content.Object, folder.Object,
            validator.Object, prePost.Object, misskey.Object,
            runner.Object, output.Object,
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
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet", Enabled = true },
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync("hello", It.IsAny<List<string>>(), config)).ReturnsAsync(DefaultSubResult());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner, mockOutput);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config), Times.Once);
        mockRunner.Verify(x => x.RunAsync("hello", It.IsAny<List<string>>(), config), Times.Once);
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner, mockOutput);
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner, mockOutput);
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner,
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_投稿後に画像削除サービスを呼ぶ()
    {
        // Arrange
        // Misskey投稿成功後、サブ投稿先の実行が完了した時点で IImageCleanupService.RunAsync が validPaths で呼ばれること。
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockCleanup = new Mock<IImageCleanupService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner,
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(DefaultSubResult());
        var mockCleanup = new Mock<IImageCleanupService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner,
            new Mock<IOutputWriter>(), cleanup: mockCleanup);

        // Act
        await app.RunAsync(args, config);

        // Assert
        mockCleanup.Verify(x => x.RunAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_X投稿失敗時も画像削除サービスは呼ばれる()
    {
        // Arrange
        // Misskey投稿が主投稿先であり、Xの起動失敗はサブ投稿先の失敗にすぎない。
        // Misskey投稿済みの状態を許容するため、画像削除の確認は行う（第7.節 部分実行に関する方針）。
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(new SubDestinationsResult(Mixi2PostResult.Skipped(), XPostLaunchResult.Failure("X起動失敗")));
        var mockCleanup = new Mock<IImageCleanupService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner,
            new Mock<IOutputWriter>(), cleanup: mockCleanup);

        // Act
        var result = await app.RunAsync(args, config);

        // Assert
        Assert.Equal(0, result);
        mockCleanup.Verify(x => x.RunAsync(It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task 正常系_MIXI2投稿成功時に結果を出力する()
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
        var mockRunner = new Mock<IPostDestinationRunner>();
        mockRunner.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()))
            .ReturnsAsync(new SubDestinationsResult(Mixi2PostResult.Success("mixi2_post_1"), XPostLaunchResult.Success()));
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockRunner, mockOutput);
        await app.RunAsync(args, config);

        mockOutput.Verify(x => x.WriteSuccess(It.Is<string>(s => s.Contains("MIXI2") && s.Contains("mixi2_post_1"))), Times.Once);
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
            new Mock<IPostDestinationRunner>(), mockOutput);

        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains(AppVersion.Current))), Times.Once);
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
            Mock.Of<IPostDestinationRunner>(), mockOutput.Object,
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
            Mock.Of<IPostDestinationRunner>(), mockOutput.Object,
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
            Mock.Of<IPostDestinationRunner>(), mockOutput.Object,
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
            Mock.Of<IPostDestinationRunner>(), mockOutput.Object,
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
            Mock.Of<IPostDestinationRunner>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 異常系_Misskey投稿失敗時はサブ投稿先を実行しない()
    {
        var args = new[] { "hello" };
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet", Enabled = true },
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
        var mockRunner = new Mock<IPostDestinationRunner>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, mockFolder.Object,
            mockValidator.Object, mockPrePost.Object, mockMisskey.Object,
            mockRunner.Object, Mock.Of<IOutputWriter>(),
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.NotEqual(0, result);
        mockRunner.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }
}
