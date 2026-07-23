using System.Linq;
using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Gui;
using IchiPos.Images;
using IchiPos.Output;
using Moq;
using Xunit;

namespace IchiPos.Tests.Gui;

public class MainWindowViewModelTests
{
    private static AppConfig ValidConfig() => new AppConfig
    {
        Misskey = new MisskeyConfig { InstanceUrl = "https://misskey.example.com", AccessToken = "test_token", Visibility = "public" },
        X = new XConfig { PostUrlBase = "https://twitter.com/intent/tweet" },
        Limits = new LimitsConfig { MisskeyMaxLength = 5000, XMaxLength = 280 }
    };

    private static MainWindowViewModel BuildViewModel(
        Mock<IIchiPosApplication>? app = null,
        Mock<ITextFileReader>? textFileReader = null,
        GuiOutputWriter? outputWriter = null,
        AppConfig? config = null,
        Mock<IClipboardImageStore>? clipboardImageStore = null,
        Mock<IImageFolderReader>? imageFolderReader = null,
        IDatePlaceholderReplacer? datePlaceholderReplacer = null,
        Mock<ILastPostStore>? lastPostStore = null,
        Mock<IRepostConfirmation>? repostConfirmation = null) =>
        new MainWindowViewModel(
            (app ?? new Mock<IIchiPosApplication>()).Object,
            config ?? ValidConfig(),
            (textFileReader ?? new Mock<ITextFileReader>()).Object,
            outputWriter ?? new GuiOutputWriter(),
            (clipboardImageStore ?? new Mock<IClipboardImageStore>()).Object,
            (imageFolderReader ?? new Mock<IImageFolderReader>()).Object,
            datePlaceholderReplacer ?? new DatePlaceholderReplacer(TimeProvider.System),
            (lastPostStore ?? new Mock<ILastPostStore>()).Object,
            (repostConfirmation ?? new Mock<IRepostConfirmation>()).Object);

    // ──────────────────────────────────────────────────────────────────
    // 初期状態(04書「画面項目一覧」節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 初期状態_投稿内容は空文字()
    {
        var vm = BuildViewModel();
        Assert.Equal(string.Empty, vm.Content);
    }

    [Fact]
    public void 初期状態_添付画像はなし()
    {
        var vm = BuildViewModel();
        Assert.Empty(vm.AttachedImages);
    }

    [Fact]
    public void 初期状態_投稿中ではない()
    {
        var vm = BuildViewModel();
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void 初期状態_バージョン文字列にバージョン番号を含む()
    {
        var vm = BuildViewModel();
        Assert.Contains(AppVersion.Current, vm.VersionText);
    }

    // ──────────────────────────────────────────────────────────────────
    // 文字数表示(P-02, 04書「画面項目一覧」節・G-002 第5節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_文字数表示は現在の文字数と上限を表示する()
    {
        // config.Limitsの短い方(XMaxLength=280)を上限として表示する(F-006と同じ算出方法)
        var vm = BuildViewModel();
        vm.Content = "hello";

        Assert.Equal("5 / 280 文字", vm.CharacterCountDisplay);
    }

    [Fact]
    public void 正常系_Content変更時にCharacterCountDisplayのPropertyChangedが発火する()
    {
        var vm = BuildViewModel();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Content = "hello";

        Assert.Contains(nameof(MainWindowViewModel.CharacterCountDisplay), raised);
    }

    // ──────────────────────────────────────────────────────────────────
    // IsNotBusy(View側のIsEnabledバインディング用)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 正常系_投稿中はIsNotBusyがfalseになる()
    {
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        Assert.False(vm.IsNotBusy);

        tcs.SetResult(0);
        await postTask;

        Assert.True(vm.IsNotBusy);
    }

    // ──────────────────────────────────────────────────────────────────
    // 投稿内容クリア(04書 G-012)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_ClearContentCommandで投稿内容を空にする()
    {
        var vm = BuildViewModel();
        vm.Content = "消えるべき投稿内容";

        vm.ClearContentCommand.Execute(null);

        Assert.Equal(string.Empty, vm.Content);
    }

    [Fact]
    public void 正常系_ClearContentCommandは添付画像とログには影響しない()
    {
        var outputWriter = new GuiOutputWriter();
        outputWriter.WriteInfo("残るべきメッセージ");
        var vm = BuildViewModel(outputWriter: outputWriter);
        vm.PasteFiles(new[] { @"C:\real\a.png" });
        vm.Content = "消えるべき投稿内容";

        vm.ClearContentCommand.Execute(null);

        Assert.Single(vm.AttachedImages);
        Assert.Single(vm.LogEntries);
    }

    // ──────────────────────────────────────────────────────────────────
    // 全クリア(04書 G-013)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_ClearAllCommandで投稿内容と添付画像とログを全て消去する()
    {
        var outputWriter = new GuiOutputWriter();
        outputWriter.WriteInfo("消えるべきメッセージ");
        var vm = BuildViewModel(outputWriter: outputWriter);
        vm.PasteFiles(new[] { @"C:\real\a.png" });
        vm.Content = "消えるべき投稿内容";

        vm.ClearAllCommand.Execute(null);

        Assert.Equal(string.Empty, vm.Content);
        Assert.Empty(vm.AttachedImages);
        Assert.Empty(vm.LogEntries);
    }

    [Fact]
    public void 正常系_ClearAllCommandで添付画像の一時ファイルを削除する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());
        vm.PasteFiles(new[] { @"C:\real\a.png" });

        vm.ClearAllCommand.Execute(null);

        mockStore.Verify(x => x.Delete(@"C:\temp\paste1\pasted.png"), Times.Once);
    }

    [Fact]
    public void 正常系_ClearAllCommandでFocusContentRequestedイベントが発火する()
    {
        var vm = BuildViewModel();
        var raised = false;
        vm.FocusContentRequested += (_, _) => raised = true;

        vm.ClearAllCommand.Execute(null);

        Assert.True(raised);
    }

    // ──────────────────────────────────────────────────────────────────
    // ログクリア(04書 G-006 第5節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_ClearLogCommandでログをすべて消去する()
    {
        var outputWriter = new GuiOutputWriter();
        outputWriter.WriteInfo("消えるべきメッセージ");
        var vm = BuildViewModel(outputWriter: outputWriter);

        vm.ClearLogCommand.Execute(null);

        Assert.Empty(vm.LogEntries);
    }

    // ──────────────────────────────────────────────────────────────────
    // 投稿実行(04書 G-005, G-007)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 正常系_PostAsyncでApplication層に投稿内容と添付画像パスの一覧を渡す()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { @"C:\temp\paste1\pasted.png" })), config)).ReturnsAsync(0);
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(app: mockApp, config: config, clipboardImageStore: mockStore);
        vm.Content = "hello";
        vm.PasteImage(DummyImage());

        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync("hello", It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { @"C:\temp\paste1\pasted.png" })), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_添付画像なし時は空リストを渡す()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.Is<IReadOnlyList<string>>(p => p.Count == 0), config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "hello";

        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync("hello", It.Is<IReadOnlyList<string>>(p => p.Count == 0), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_投稿中はIsBusyがtrueになり完了後falseに戻る()
    {
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        Assert.True(vm.IsBusy);

        tcs.SetResult(0);
        await postTask;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task 正常系_投稿中はPostCommandを実行できない()
    {
        // 04書 G-007: 二重投稿防止
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp);
        vm.Content = "hello";

        Assert.True(vm.PostCommand.CanExecute(null));

        var postTask = vm.PostAsync();
        Assert.False(vm.PostCommand.CanExecute(null));

        tcs.SetResult(0);
        await postTask;

        Assert.True(vm.PostCommand.CanExecute(null));
    }

    // ──────────────────────────────────────────────────────────────────
    // ファイルからの読み込み(04書 G-002 第4節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task 正常系_ファイル読み込み成功時は投稿内容を置き換える()
    {
        var mockReader = new Mock<ITextFileReader>();
        mockReader.Setup(x => x.ReadAsync(@"C:\post.txt")).ReturnsAsync(TextFileReadResult.Success("ファイルの内容"));
        var vm = BuildViewModel(textFileReader: mockReader);
        vm.Content = "編集前の内容";

        await vm.LoadContentFromFileAsync(@"C:\post.txt");

        Assert.Equal("ファイルの内容", vm.Content);
    }

    [Fact]
    public async Task 異常系_ファイル読み込み失敗時は投稿内容を変更せずエラーをログに出す()
    {
        var mockReader = new Mock<ITextFileReader>();
        mockReader.Setup(x => x.ReadAsync(@"C:\missing.txt")).ReturnsAsync(TextFileReadResult.Failure("ファイルが存在しません"));
        var outputWriter = new GuiOutputWriter();
        var vm = BuildViewModel(textFileReader: mockReader, outputWriter: outputWriter);
        vm.Content = "編集前の内容";

        await vm.LoadContentFromFileAsync(@"C:\missing.txt");

        Assert.Equal("編集前の内容", vm.Content);
        Assert.Contains(outputWriter.Entries, e => e.Severity == LogSeverity.Error && e.Message.Contains("ファイルが存在しません"));
    }

    // ──────────────────────────────────────────────────────────────────
    // フォルダ選択(04書 G-003)
    // ──────────────────────────────────────────────────────────────────

    private static System.Windows.Media.Imaging.BitmapSource DummyImage() =>
        new System.Windows.Media.Imaging.WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

    [Fact]
    public void 正常系_SetImagesFromFolderでフォルダ内の画像がソート順に追加される()
    {
        var mockFolderReader = new Mock<IImageFolderReader>();
        mockFolderReader.Setup(x => x.Read(@"C:\images"))
            .Returns(ImageFolderReadResult.Success(new List<string> { "a.png", "b.png" }));
        var vm = BuildViewModel(imageFolderReader: mockFolderReader);

        vm.SetImagesFromFolder(@"C:\images");

        Assert.Equal(new[] { @"C:\images\a.png", @"C:\images\b.png" }, vm.AttachedImages.Select(i => i.FilePath));
        Assert.All(vm.AttachedImages, i => Assert.False(i.IsTemporary));
    }

    [Fact]
    public void 正常系_SetImagesFromFolderは既存リストを全てクリアしてから置き換える()
    {
        var mockFolderReader = new Mock<IImageFolderReader>();
        mockFolderReader.Setup(x => x.Read(@"C:\images1")).Returns(ImageFolderReadResult.Success(new List<string> { "old.png" }));
        mockFolderReader.Setup(x => x.Read(@"C:\images2")).Returns(ImageFolderReadResult.Success(new List<string> { "new.png" }));
        var vm = BuildViewModel(imageFolderReader: mockFolderReader);
        vm.SetImagesFromFolder(@"C:\images1");

        vm.SetImagesFromFolder(@"C:\images2");

        Assert.Equal(new[] { @"C:\images2\new.png" }, vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public void 正常系_SetImagesFromFolderで一時ファイルはクリア時に削除される()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var mockFolderReader = new Mock<IImageFolderReader>();
        mockFolderReader.Setup(x => x.Read(@"C:\images")).Returns(ImageFolderReadResult.Success(new List<string>()));
        var vm = BuildViewModel(clipboardImageStore: mockStore, imageFolderReader: mockFolderReader);
        vm.PasteImage(DummyImage());

        vm.SetImagesFromFolder(@"C:\images");

        mockStore.Verify(x => x.Delete(@"C:\temp\paste1\pasted.png"), Times.Once);
    }

    [Fact]
    public void 異常系_SetImagesFromFolderがエラーの場合はリストを変更しない()
    {
        var mockFolderReader = new Mock<IImageFolderReader>();
        mockFolderReader.Setup(x => x.Read(@"C:\images1")).Returns(ImageFolderReadResult.Success(new List<string> { "a.png" }));
        mockFolderReader.Setup(x => x.Read(@"C:\missing")).Returns(ImageFolderReadResult.Failure("フォルダが存在しません"));
        var outputWriter = new GuiOutputWriter();
        var vm = BuildViewModel(imageFolderReader: mockFolderReader, outputWriter: outputWriter);
        vm.SetImagesFromFolder(@"C:\images1");

        vm.SetImagesFromFolder(@"C:\missing");

        Assert.Equal(new[] { @"C:\images1\a.png" }, vm.AttachedImages.Select(i => i.FilePath));
        Assert.Contains(outputWriter.Entries, e => e.Severity == LogSeverity.Error);
    }

    // ──────────────────────────────────────────────────────────────────
    // クリップボード画像貼り付け(04書 G-010): 追加(置き換えではない)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_画像を貼り付けると添付画像一覧に追加される()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);

        vm.PasteImage(DummyImage());

        var image = Assert.Single(vm.AttachedImages);
        Assert.Equal(@"C:\temp\paste1\pasted.png", image.FilePath);
        Assert.True(image.IsTemporary);
    }

    [Fact]
    public void 正常系_続けて貼り付けると前回の画像を置き換えずに追加される()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.SetupSequence(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>()))
            .Returns(@"C:\temp\paste1\pasted.png")
            .Returns(@"C:\temp\paste2\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);

        vm.PasteImage(DummyImage());
        vm.PasteImage(DummyImage());

        Assert.Equal(
            new[] { @"C:\temp\paste1\pasted.png", @"C:\temp\paste2\pasted.png" },
            vm.AttachedImages.Select(i => i.FilePath));
        mockStore.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_投稿中は貼り付けを無視する()
    {
        // 04書 G-010 第6節
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var mockStore = new Mock<IClipboardImageStore>();
        var vm = BuildViewModel(app: mockApp, clipboardImageStore: mockStore);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        vm.PasteImage(DummyImage());

        mockStore.Verify(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>()), Times.Never);
        Assert.Empty(vm.AttachedImages);

        tcs.SetResult(0);
        await postTask;
    }

    // ──────────────────────────────────────────────────────────────────
    // 複数ファイルのクリップボード貼り付け(04書 G-010、issue #13)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_PasteFilesで対応拡張子のファイルが追加される()
    {
        var vm = BuildViewModel();

        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.jpg" });

        Assert.Equal(
            new[] { @"C:\real\a.png", @"C:\real\b.jpg" },
            vm.AttachedImages.Select(i => i.FilePath));
        Assert.All(vm.AttachedImages, i => Assert.False(i.IsTemporary));
    }

    [Fact]
    public void 正常系_PasteFilesは既存の一覧に追加する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());

        vm.PasteFiles(new[] { @"C:\real\a.png" });

        Assert.Equal(
            new[] { @"C:\temp\paste1\pasted.png", @"C:\real\a.png" },
            vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public void 正常系_PasteFilesで非対応拡張子は除外され警告ログにファイル名が表示される()
    {
        var outputWriter = new GuiOutputWriter();
        var vm = BuildViewModel(outputWriter: outputWriter);

        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\readme.txt" });

        Assert.Equal(new[] { @"C:\real\a.png" }, vm.AttachedImages.Select(i => i.FilePath));
        Assert.Contains(outputWriter.Entries, e => e.Severity == LogSeverity.Warning && e.Message.Contains("readme.txt"));
    }

    [Fact]
    public async Task 正常系_投稿中はPasteFilesを無視する()
    {
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        vm.PasteFiles(new[] { @"C:\real\a.png" });

        Assert.Empty(vm.AttachedImages);

        tcs.SetResult(0);
        await postTask;
    }

    // ──────────────────────────────────────────────────────────────────
    // 個別削除・全削除(04書 G-013)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_RemoveImageCommandで指定した1件だけ除去する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.SetupSequence(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>()))
            .Returns(@"C:\temp\paste1\pasted.png")
            .Returns(@"C:\temp\paste2\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());
        vm.PasteImage(DummyImage());
        var target = vm.AttachedImages[0];

        vm.RemoveImageCommand.Execute(target);

        Assert.Equal(new[] { @"C:\temp\paste2\pasted.png" }, vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public void 正常系_RemoveImageCommandで一時ファイルは実削除する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());
        var target = vm.AttachedImages[0];

        vm.RemoveImageCommand.Execute(target);

        mockStore.Verify(x => x.Delete(@"C:\temp\paste1\pasted.png"), Times.Once);
    }

    [Fact]
    public void 正常系_RemoveImageCommandで実ファイル由来は削除しない()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteFiles(new[] { @"C:\real\a.png" });
        var target = vm.AttachedImages[0];

        vm.RemoveImageCommand.Execute(target);

        Assert.Empty(vm.AttachedImages);
        mockStore.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void 正常系_ClearImagesCommandで全て除去し一時ファイルのみ削除する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFile(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1\pasted.png");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());
        vm.PasteFiles(new[] { @"C:\real\a.png" });

        vm.ClearImagesCommand.Execute(null);

        Assert.Empty(vm.AttachedImages);
        mockStore.Verify(x => x.Delete(@"C:\temp\paste1\pasted.png"), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // サムネイルのドラッグ&ドロップ並べ替え(04書 G-011)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_MoveImageで指定位置に移動する()
    {
        var vm = BuildViewModel();
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png", @"C:\real\c.png" });

        vm.MoveImage(0, 2);

        Assert.Equal(
            new[] { @"C:\real\b.png", @"C:\real\c.png", @"C:\real\a.png" },
            vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public void 正常系_MoveImageで末尾から先頭へ移動できる()
    {
        var vm = BuildViewModel();
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png", @"C:\real\c.png" });

        vm.MoveImage(2, 0);

        Assert.Equal(
            new[] { @"C:\real\c.png", @"C:\real\a.png", @"C:\real\b.png" },
            vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public void 正常系_MoveImageで同じ位置を指定した場合は順序が変わらない()
    {
        var vm = BuildViewModel();
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png" });

        vm.MoveImage(1, 1);

        Assert.Equal(
            new[] { @"C:\real\a.png", @"C:\real\b.png" },
            vm.AttachedImages.Select(i => i.FilePath));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    public void 異常系_範囲外のインデックスは無視する(int fromIndex, int toIndex)
    {
        var vm = BuildViewModel();
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png" });

        vm.MoveImage(fromIndex, toIndex);

        Assert.Equal(
            new[] { @"C:\real\a.png", @"C:\real\b.png" },
            vm.AttachedImages.Select(i => i.FilePath));
    }

    [Fact]
    public async Task 正常系_投稿中はMoveImageを無視する()
    {
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp);
        vm.Content = "hello";
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png" });

        var postTask = vm.PostAsync();
        vm.MoveImage(0, 1);

        Assert.Equal(
            new[] { @"C:\real\a.png", @"C:\real\b.png" },
            vm.AttachedImages.Select(i => i.FilePath));

        tcs.SetResult(0);
        await postTask;
    }

    [Fact]
    public async Task 正常系_MoveImage後のPostAsyncは並べ替え後の順序で渡す()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "hello";
        vm.PasteFiles(new[] { @"C:\real\a.png", @"C:\real\b.png" });

        vm.MoveImage(0, 1);
        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync(
            "hello",
            It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { @"C:\real\b.png", @"C:\real\a.png" })),
            config), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // 同一内容再投稿確認(04書 G-015)
    // ──────────────────────────────────────────────────────────────────

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static DatePlaceholderReplacer ReplacerAt(int year, int month, int day)
        => new DatePlaceholderReplacer(new FixedTimeProvider(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero)));

    private static string HashOf(string comparableContent) => PostContentHash.Compute(comparableContent);

    [Fact]
    public async Task 正常系_前回投稿の記録がない場合は確認せず投稿する()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns((string?)null);
        var confirmation = new Mock<IRepostConfirmation>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Never);
        mockApp.Verify(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_前回と異なる内容の場合は確認せず投稿する()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("別の内容"));
        var confirmation = new Mock<IRepostConfirmation>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Never);
        mockApp.Verify(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_前回と同じ内容で確認にはいと答えた場合は投稿する()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(true);
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Once);
        mockApp.Verify(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_前回と同じ内容で確認にいいえと答えた場合は投稿しない()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_確認にいいえと答えた場合は結果ログに中止メッセージを出す()
    {
        var outputWriter = new GuiOutputWriter();
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(outputWriter: outputWriter, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        Assert.Contains(outputWriter.Entries, e => e.Message.Contains("投稿を中止しました"));
    }

    [Fact]
    public async Task 正常系_確認にいいえと答えた場合も投稿内容と添付画像は保持される()
    {
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";
        vm.PasteFiles(new[] { @"C:\real\a.png" });

        await vm.PostAsync();

        Assert.Equal("hello", vm.Content);
        Assert.Single(vm.AttachedImages);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task 正常系_確認にいいえと答えた場合は前回投稿内容を更新しない()
    {
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello";

        await vm.PostAsync();

        store.Verify(x => x.SaveHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_投稿成功時は前回投稿内容として記録する()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store);
        vm.Content = "hello";

        await vm.PostAsync();

        store.Verify(x => x.SaveHash(HashOf("hello")), Times.Once);
    }

    [Fact]
    public async Task 異常系_投稿失敗時は前回投稿内容を記録しない()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(1);
        var store = new Mock<ILastPostStore>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store);
        vm.Content = "hello";

        await vm.PostAsync();

        store.Verify(x => x.SaveHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_比較用テキストは末尾の空白改行を除去してから判定する()
    {
        // F-006の末尾トリムと同じ形に揃えるため、"hello" と "hello  \r\n" は同一内容とみなす
        var mockApp = new Mock<IIchiPosApplication>();
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("hello"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(app: mockApp, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "hello  \r\n";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Once);
        mockApp.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_比較用テキストは日付プレースホルダを置換してから判定する()
    {
        // {date}を含む投稿は、置換後の日付が同じ日であれば同一内容とみなす(G-015第2節)
        var mockApp = new Mock<IIchiPosApplication>();
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("今日は2026/07/19です"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var vm = BuildViewModel(
            app: mockApp,
            datePlaceholderReplacer: ReplacerAt(2026, 7, 19),
            lastPostStore: store,
            repostConfirmation: confirmation);
        vm.Content = "今日は{date}です";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Once);
    }

    [Fact]
    public async Task 正常系_日付プレースホルダの置換結果が異なる日なら同一とみなさない()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("今日は2026/07/19です"));
        var confirmation = new Mock<IRepostConfirmation>();
        var vm = BuildViewModel(
            app: mockApp,
            config: config,
            datePlaceholderReplacer: ReplacerAt(2026, 7, 20),
            lastPostStore: store,
            repostConfirmation: confirmation);
        vm.Content = "今日は{date}です";

        await vm.PostAsync();

        confirmation.Verify(x => x.ConfirmRepost(), Times.Never);
        store.Verify(x => x.SaveHash(HashOf("今日は2026/07/20です")), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // 定型文投稿(04書 G-016)
    // ──────────────────────────────────────────────────────────────────

    private static AppConfig ConfigWithTemplates(params string[] templates)
    {
        var config = ValidConfig();
        config.Templates = templates.ToList();
        return config;
    }

    [Fact]
    public void 初期状態_定型文一覧は設定の内容を登録順のまま公開する()
    {
        var vm = BuildViewModel(config: ConfigWithTemplates("おはよう", "おやすみ"));

        Assert.Equal(new[] { "おはよう", "おやすみ" }, vm.Templates);
    }

    [Fact]
    public void 初期状態_定型文が0件ならHasTemplatesがfalse()
    {
        // G-016第3節第4項: 一覧の代わりに未登録の案内を表示するためのView側の判定に使う。
        var vm = BuildViewModel(config: ConfigWithTemplates());

        Assert.False(vm.HasTemplates);
    }

    [Fact]
    public void 初期状態_定型文が1件以上あればHasTemplatesがtrue()
    {
        var vm = BuildViewModel(config: ConfigWithTemplates("おはよう"));

        Assert.True(vm.HasTemplates);
    }

    [Fact]
    public async Task 正常系_定型文投稿は定型文テキストを投稿内容として渡す()
    {
        var config = ConfigWithTemplates("おはよう", "おやすみ");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);

        await vm.PostTemplateAsync("おやすみ");

        mockApp.Verify(x => x.RunAsync("おやすみ", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_定型文投稿は添付画像を渡さない()
    {
        // G-016第4節第4項: 添付画像一覧に画像が残っていても定型文投稿には添付しない。
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.PasteFiles(new[] { @"C:\images\a.png" });

        await vm.PostTemplateAsync("おはよう");

        mockApp.Verify(x => x.RunAsync("おはよう", It.Is<IReadOnlyList<string>>(p => p.Count == 0), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_定型文投稿は投稿内容欄と添付画像一覧を変更しない()
    {
        // G-016第4.1節: 押下時点で投稿される内容が定型文テキストのみに一意に定まることを保証する。
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "入力途中のテキスト";
        vm.PasteFiles(new[] { @"C:\images\a.png" });

        await vm.PostTemplateAsync("おはよう");

        Assert.Equal("入力途中のテキスト", vm.Content);
        Assert.Single(vm.AttachedImages);
        Assert.Equal(@"C:\images\a.png", vm.AttachedImages[0].FilePath);
    }

    [Fact]
    public async Task 正常系_定型文投稿中はIsBusyがtrueになる()
    {
        // G-016第5節第1項: 二重投稿防止(G-007)の対象とする。
        var tcs = new TaskCompletionSource<int>();
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp, config: config);

        var postTask = vm.PostTemplateAsync("おはよう");
        Assert.True(vm.IsBusy);
        Assert.False(vm.IsNotBusy);

        tcs.SetResult(0);
        await postTask;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task 正常系_投稿中は定型文投稿コマンドを実行できない()
    {
        // G-016第5節第1項: 通常の投稿の実行中は全ての定型文の投稿ボタンを無効化する。
        var tcs = new TaskCompletionSource<int>();
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        Assert.False(vm.PostTemplateCommand.CanExecute("おはよう"));

        tcs.SetResult(0);
        await postTask;

        Assert.True(vm.PostTemplateCommand.CanExecute("おはよう"));
    }

    [Fact]
    public async Task 正常系_定型文投稿の実行中は通常の投稿コマンドを実行できない()
    {
        var tcs = new TaskCompletionSource<int>();
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config))
            .Returns(tcs.Task);
        var vm = BuildViewModel(app: mockApp, config: config);

        var postTask = vm.PostTemplateAsync("おはよう");
        Assert.False(vm.PostCommand.CanExecute(null));

        tcs.SetResult(0);
        await postTask;

        Assert.True(vm.PostCommand.CanExecute(null));
    }

    [Fact]
    public async Task 正常系_定型文投稿が成功したら前回投稿内容として記録する()
    {
        // G-016第5節第2項: 前回投稿内容の記録は通常の投稿と共有する。
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store);

        await vm.PostTemplateAsync("おはよう");

        store.Verify(x => x.SaveHash(HashOf("おはよう")), Times.Once);
    }

    [Fact]
    public async Task 正常系_定型文投稿が失敗したら前回投稿内容を記録しない()
    {
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(1);
        var store = new Mock<ILastPostStore>();
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store);

        await vm.PostTemplateAsync("おはよう");

        store.Verify(x => x.SaveHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_前回と同じ定型文なら再投稿確認を行う()
    {
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("おはよう"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(true);
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);

        await vm.PostTemplateAsync("おはよう");

        confirmation.Verify(x => x.ConfirmRepost(), Times.Once);
        mockApp.Verify(x => x.RunAsync("おはよう", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_定型文の再投稿確認でいいえを選ぶと投稿しない()
    {
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        var store = new Mock<ILastPostStore>();
        store.Setup(x => x.LoadHash()).Returns(HashOf("おはよう"));
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(false);
        var outputWriter = new GuiOutputWriter();
        var vm = BuildViewModel(
            app: mockApp,
            config: config,
            outputWriter: outputWriter,
            lastPostStore: store,
            repostConfirmation: confirmation);

        await vm.PostTemplateAsync("おはよう");

        mockApp.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<AppConfig>()), Times.Never);
        Assert.Contains(outputWriter.Entries, e => e.Message == "投稿を中止しました");
    }

    [Fact]
    public async Task 正常系_通常の投稿の直後に同一内容の定型文を投稿すると確認する()
    {
        // G-016第5節第2項: 前回投稿内容の記録は投稿経路ごとに分けない。
        var config = ConfigWithTemplates("おはよう");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        // 保存したハッシュをそのまま読み出し、投稿をまたいだ記録の共有を再現する。
        var store = new Mock<ILastPostStore>();
        string? savedHash = null;
        store.Setup(x => x.SaveHash(It.IsAny<string>())).Callback<string>(h => savedHash = h);
        store.Setup(x => x.LoadHash()).Returns(() => savedHash);
        var confirmation = new Mock<IRepostConfirmation>();
        confirmation.Setup(x => x.ConfirmRepost()).Returns(true);
        var vm = BuildViewModel(app: mockApp, config: config, lastPostStore: store, repostConfirmation: confirmation);
        vm.Content = "おはよう";

        await vm.PostAsync();
        await vm.PostTemplateAsync("おはよう");

        confirmation.Verify(x => x.ConfirmRepost(), Times.Once);
    }

    [Fact]
    public async Task 正常系_定型文の日付プレースホルダは投稿実行時に置換される()
    {
        var config = ConfigWithTemplates("今日は{date}です");
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), config)).ReturnsAsync(0);
        var store = new Mock<ILastPostStore>();
        var vm = BuildViewModel(
            app: mockApp,
            config: config,
            datePlaceholderReplacer: ReplacerAt(2026, 7, 20),
            lastPostStore: store);

        await vm.PostTemplateAsync("今日は{date}です");

        // 置換は投稿処理層(F-013)が行うため、渡すのは未置換のテキストのまま。
        // 記録するハッシュは置換後のテキストで計算する(G-015第2節)。
        mockApp.Verify(x => x.RunAsync("今日は{date}です", It.IsAny<IReadOnlyList<string>>(), config), Times.Once);
        store.Verify(x => x.SaveHash(HashOf("今日は2026/07/20です")), Times.Once);
    }
}
