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
        Mock<IImageCleanupService>? cleanup = null,
        Mock<IDatePlaceholderReplacer>? datePlaceholder = null) =>
        new IchiPosApplication(
            parser.Object, content.Object, datePlaceholder?.Object ?? Mock.Of<IDatePlaceholderReplacer>(),
            folder.Object, validator.Object, prePost.Object, misskey.Object,
            x.Object, output.Object,
            clipboard?.Object ?? Mock.Of<IClipboardService>(),
            cleanup?.Object ?? Mock.Of<IImageCleanupService>());

    // GUI入力（RunAsync(content, imagePath, config)）のテストで使う、そのまま返す日付置換モック
    private static Mock<IDatePlaceholderReplacer> PassthroughDatePlaceholder()
    {
        var mock = new Mock<IDatePlaceholderReplacer>();
        mock.Setup(x => x.Replace(It.IsAny<string>())).Returns((string s) => s);
        return mock;
    }

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
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        await app.RunAsync(args, config);

        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("添付画像") && s.Contains("2"))), Times.Once);
    }

    [Fact]
    public async Task 正常系_X投稿成功後に画像をクリップボードにコピーする()
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImages(
            It.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(validPaths))), Times.Once);
    }

    [Fact]
    public async Task 正常系_添付画像が4枚を超える場合は先頭4枚のみクリップボードにコピーする()
    {
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images"));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        var fileNames = new List<string> { "a.png", "b.png", "c.png", "d.png", "e.png" };
        mockFolder.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(fileNames));
        var validPaths = fileNames.Select(f => $@"C:\images\{f}").ToList();
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(validPaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImages(
            It.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(validPaths.Take(4)))), Times.Once);
    }

    [Fact]
    public async Task 正常系_クリップボードコピー失敗時はエラーを出し成功ログを出さない()
    {
        // issue #56: コピーが失敗しても「コピーしました」と出ていた。失敗時はエラーを出し、
        // 成功ログは出さない。Misskey投稿は成功済みなので終了コードは0のまま。
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();
        mockClipboard.Setup(x => x.SetImages(It.IsAny<IReadOnlyList<string>>()))
            .Throws(new ClipboardCopyException("クリップボードへのコピーに失敗しました"));
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            mockOutput, mockClipboard);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockOutput.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("コピー"))), Times.Once);
        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("コピーしました"))), Times.Never);
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockClipboard = new Mock<IClipboardService>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>(), mockClipboard);
        await app.RunAsync(args, config);

        mockClipboard.Verify(x => x.SetImages(It.IsAny<IReadOnlyList<string>>()), Times.Never);
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
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
        mockOutput.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains(AppVersion.Current))), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // 異常系
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 正常系_CLI入力_投稿テキスト末尾の空白改行がトリムされて投稿される()
    {
        // F-006: 投稿直前に文末の空白・改行だけを除去する（自動トリミング対象外の例外）。
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
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("hello \r\n"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("hello", config), Times.Once);
    }

    [Fact]
    public async Task 正常系_CLI入力_文頭の空白はトリムされない()
    {
        var args = new[] { "hello" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", null));
        var mockContent = new Mock<IContentResolver>();
        mockContent.Setup(x => x.ResolveAsync("hello")).ReturnsAsync(ContentResolveResult.Success("  hello"));
        var mockFolder = new Mock<IImageFolderReader>();
        mockFolder.Setup(x => x.Read(null)).Returns(ImageFolderReadResult.Success(new List<string>()));
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("  hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("  hello", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX, mockOutput);
        var result = await app.RunAsync(args, config);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("  hello", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task 異常系_コマンドライン引数エラー()
    {
        var args = Array.Empty<string>();
        var config = new AppConfig();

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Failure("引数エラー"));
        var mockOutput = new Mock<IOutputWriter>();

        var app = new IchiPosApplication(
            mockParser.Object, Mock.Of<IContentResolver>(), Mock.Of<IDatePlaceholderReplacer>(), Mock.Of<IImageFolderReader>(),
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
            mockParser.Object, mockContent.Object, Mock.Of<IDatePlaceholderReplacer>(), Mock.Of<IImageFolderReader>(),
            Mock.Of<IImageValidator>(), Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
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
            mockParser.Object, mockContent.Object, Mock.Of<IDatePlaceholderReplacer>(), mockFolder.Object,
            Mock.Of<IImageValidator>(), Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
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
            mockParser.Object, mockContent.Object, Mock.Of<IDatePlaceholderReplacer>(), mockFolder.Object,
            mockValidator.Object, Mock.Of<IPrePostValidator>(), mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
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
            mockParser.Object, mockContent.Object, Mock.Of<IDatePlaceholderReplacer>(), mockFolder.Object,
            mockValidator.Object, mockPrePost.Object, mockMisskey.Object,
            Mock.Of<IXPostLauncher>(), mockOutput.Object,
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
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
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Failure("Misskey投稿失敗"));
        var mockX = new Mock<IXPostLauncher>();

        var app = new IchiPosApplication(
            mockParser.Object, mockContent.Object, Mock.Of<IDatePlaceholderReplacer>(), mockFolder.Object,
            mockValidator.Object, mockPrePost.Object, mockMisskey.Object,
            mockX.Object, Mock.Of<IOutputWriter>(),
            Mock.Of<IClipboardService>(), Mock.Of<IImageCleanupService>());

        var result = await app.RunAsync(args, config);

        Assert.NotEqual(0, result);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // GUI入力（RunAsync(content, imagePaths, config)、04書 G-005）
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GUI入力_正常系_文字列を直接投稿する()
    {
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockContent = new Mock<IContentResolver>();
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), mockContent, new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("hello", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("hello", config), Times.Once);
    }

    [Fact]
    public async Task GUI入力_正常系_txt拡張子の文字列でもファイルとして扱わない()
    {
        // 04書 G-005 第3節: GUI入力は常に文字列として扱い、.txt判定（F-002）は行わない。
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockContent = new Mock<IContentResolver>();
        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello.txt", It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello.txt", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), mockContent, new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("hello.txt", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(0, result);
        mockContent.Verify(x => x.ResolveAsync(It.IsAny<string>()), Times.Never);
        mockMisskey.Verify(x => x.PostAsync("hello.txt", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GUI入力_正常系_日付プレースホルダを投稿実行時に置換する()
    {
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("今日は2026/07/04です", It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("今日は2026/07/04です", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var mockDatePlaceholder = new Mock<IDatePlaceholderReplacer>();
        mockDatePlaceholder.Setup(x => x.Replace("今日は{date}です")).Returns("今日は2026/07/04です");

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: mockDatePlaceholder);

        var result = await app.RunAsync("今日は{date}です", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("今日は2026/07/04です", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GUI入力_正常系_複数の画像パスをそのまま検証投稿に渡す()
    {
        // issue #13: GUIは画像ファイルのフルパスのリストをそのまま検証・投稿へ渡す（フォルダ結合をしない）
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };
        var imagePaths = new List<string> { @"C:\folder\a.png", @"C:\temp\pasted.png" };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(imagePaths))
            .Returns(ImageValidationResult.Success(imagePaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", imagePaths, config, It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("hello", imagePaths, config, isSensitive: true);

        Assert.Equal(0, result);
        mockValidator.Verify(x => x.ValidateFiles(imagePaths), Times.Once);
        mockMisskey.Verify(x => x.PostAsync("hello", imagePaths, config, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GUI入力_正常系_テキストボックス直接入力の末尾改行がトリムされて投稿される()
    {
        // issue: X投稿画面の文末に余計な空白が入る問題（GUIテキストボックス直接入力・貼り付け経路）。
        // F-006: 入力経路によらず投稿直前に文末の空白・改行だけを除去する。
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>())).ReturnsAsync(MisskeyPostResult.Success("note_id_123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("hello", config)).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        // WPFのマルチラインTextBoxで末尾でEnterを押した場合や、他アプリからの貼り付けを想定した末尾改行。
        var result = await app.RunAsync("hello\r\n", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("hello", config), Times.Once);
    }

    [Fact]
    public async Task GUI入力_異常系_画像検証失敗時はMisskey投稿を開始しない()
    {
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };
        var imagePaths = new List<string> { @"C:\folder\broken.png" };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(imagePaths))
            .Returns(ImageValidationResult.Failure("画像として読み込めないファイルが含まれています"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            new Mock<IPrePostValidator>(), mockMisskey, new Mock<IXPostLauncher>(), mockOutput,
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("hello", imagePaths, config, isSensitive: true);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GUI入力_異常系_投稿前チェック失敗時はMisskey投稿を開始しない()
    {
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Failure("テキストが空です"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, new Mock<IXPostLauncher>(), mockOutput,
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GUI入力_異常系_Misskey投稿失敗時はX投稿画面を開かない()
    {
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("hello", It.IsAny<List<string>>(), It.IsAny<int>())).Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync("hello", It.IsAny<List<string>>(), config, It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Failure("Misskey投稿失敗"));
        var mockX = new Mock<IXPostLauncher>();

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("hello", Array.Empty<string>(), config, isSensitive: true);

        Assert.NotEqual(0, result);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // センシティブフラグ(issue #107、01書「画像のセンシティブフラグ」節・04書 G-018)
    // ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CLI入力_ParseResultのセンシティブフラグがMisskey投稿へそのまま渡る(bool isSensitive)
    {
        // CLIでは ParseResult.IsSensitive(--sensitive の解析結果)が PostAsync まで届く。
        var args = new[] { "hello", "--image-path", @"C:\images" };
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockParser = new Mock<ICommandLineParser>();
        mockParser.Setup(x => x.Parse(args)).Returns(ParseResult.Success("hello", @"C:\images", isSensitive));
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
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(mockParser, mockContent, mockFolder, mockValidator, mockPrePost, mockMisskey, mockX,
            new Mock<IOutputWriter>());
        await app.RunAsync(args, config);

        mockMisskey.Verify(x => x.PostAsync(
            It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), isSensitive), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GUI入力_センシティブフラグ引数がMisskey投稿へそのまま渡る(bool isSensitive)
    {
        // GUI(04書 G-018)では RunAsync の isSensitive 引数が PostAsync まで届く。
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };
        var imagePaths = new List<string> { @"C:\folder\a.png" };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(imagePaths))
            .Returns(ImageValidationResult.Success(imagePaths));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        await app.RunAsync("hello", imagePaths, config, isSensitive);

        mockMisskey.Verify(x => x.PostAsync("hello", imagePaths, config, isSensitive), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // 定型文: Misskey本文とX本文を分けて投稿する(04書 G-016、issue #111)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GUI入力_定型文_MisskeyにはMisskey本文_XにはX本文を投稿する_issue111()
    {
        // issue #111: 1つの投稿でMisskeyには:ohayou:を、Xにはおはようを投稿できる。
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync(":ohayou:", Array.Empty<string>(), config, isSensitive: true, xContent: "おはよう");

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync(":ohayou:", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("おはよう", config), Times.Once);
    }

    [Fact]
    public async Task GUI入力_xContent省略時はMisskeyもXも同じ本文になる_issue111()
    {
        // xContent を渡さない通常の投稿・CLI相当では、Misskey・X とも同じ本文を投稿する（後方互換）。
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        // xContent を省略（既定 null）。
        var result = await app.RunAsync("おはよう", Array.Empty<string>(), config, isSensitive: true);

        Assert.Equal(0, result);
        mockMisskey.Verify(x => x.PostAsync("おはよう", It.IsAny<List<string>>(), config, It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("おはよう", config), Times.Once);
    }

    [Fact]
    public async Task GUI入力_定型文_Misskey本文はMisskey上限_X本文はX上限で検証する_issue111()
    {
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: PassthroughDatePlaceholder());

        await app.RunAsync("みすきー本文", Array.Empty<string>(), config, isSensitive: true, xContent: "x本文");

        // Misskey本文は misskey_max_length(5000)、X本文は x_max_length(280) で検証される。
        mockPrePost.Verify(x => x.Validate("みすきー本文", It.IsAny<List<string>>(), 5000), Times.Once);
        mockPrePost.Verify(x => x.Validate("x本文", It.IsAny<List<string>>(), 280), Times.Once);
    }

    [Fact]
    public async Task GUI入力_定型文_X本文が上限超過ならMisskey投稿もX起動もしない_issue111()
    {
        // 本文ごとの上限差: Misskey本文(短い)は通るが、X本文(長い)がX上限を超える場合。
        var config = new AppConfig
        {
            Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
            X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
            Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
        };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate("短い", It.IsAny<List<string>>(), 5000))
            .Returns(PrePostValidationResult.Success());
        mockPrePost.Setup(x => x.Validate("とても長いX本文", It.IsAny<List<string>>(), 280))
            .Returns(PrePostValidationResult.Failure("投稿テキストが長さ制限を超えています（280文字以内）"));
        var mockMisskey = new Mock<IMisskeyPoster>();
        var mockX = new Mock<IXPostLauncher>();
        var mockOutput = new Mock<IOutputWriter>();

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, mockOutput,
            datePlaceholder: PassthroughDatePlaceholder());

        var result = await app.RunAsync("短い", Array.Empty<string>(), config, isSensitive: true, xContent: "とても長いX本文");

        Assert.Equal(1, result);
        mockOutput.Verify(x => x.WriteError(It.IsAny<string>()), Times.Once);
        mockMisskey.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task GUI入力_定型文_日付プレースホルダはMisskey本文とX本文の双方に適用される_issue111()
    {
        var config = new AppConfig { Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 } };

        var mockValidator = new Mock<IImageValidator>();
        mockValidator.Setup(x => x.ValidateFiles(It.IsAny<List<string>>()))
            .Returns(ImageValidationResult.Success(new List<string>()));
        var mockPrePost = new Mock<IPrePostValidator>();
        mockPrePost.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .Returns(PrePostValidationResult.Success());
        var mockMisskey = new Mock<IMisskeyPoster>();
        mockMisskey.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()))
            .ReturnsAsync(MisskeyPostResult.Success("note123"));
        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<AppConfig>())).ReturnsAsync(XPostLaunchResult.Success());

        var mockDate = new Mock<IDatePlaceholderReplacer>();
        mockDate.Setup(x => x.Replace("M{date}")).Returns("M2026/07/04");
        mockDate.Setup(x => x.Replace("X{date}")).Returns("X2026/07/04");

        var app = BuildApp(
            new Mock<ICommandLineParser>(), new Mock<IContentResolver>(), new Mock<IImageFolderReader>(), mockValidator,
            mockPrePost, mockMisskey, mockX, new Mock<IOutputWriter>(),
            datePlaceholder: mockDate);

        await app.RunAsync("M{date}", Array.Empty<string>(), config, isSensitive: true, xContent: "X{date}");

        mockMisskey.Verify(x => x.PostAsync("M2026/07/04", It.IsAny<List<string>>(), It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Once);
        mockX.Verify(x => x.LaunchAsync("X2026/07/04", It.IsAny<AppConfig>()), Times.Once);
    }
}
