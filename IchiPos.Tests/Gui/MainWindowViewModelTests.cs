using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Gui;
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
        Mock<IClipboardImageStore>? clipboardImageStore = null) =>
        new MainWindowViewModel(
            (app ?? new Mock<IIchiPosApplication>()).Object,
            config ?? ValidConfig(),
            (textFileReader ?? new Mock<ITextFileReader>()).Object,
            outputWriter ?? new GuiOutputWriter(),
            (clipboardImageStore ?? new Mock<IClipboardImageStore>()).Object);

    // ──────────────────────────────────────────────────────────────────
    // 初期状態(04書 5.3節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 初期状態_投稿内容は空文字()
    {
        var vm = BuildViewModel();
        Assert.Equal(string.Empty, vm.Content);
    }

    [Fact]
    public void 初期状態_画像フォルダパスは未設定()
    {
        var vm = BuildViewModel();
        Assert.Null(vm.ImageFolderPath);
    }

    [Fact]
    public void 初期状態_画像削除チェックはオフ()
    {
        // 04書 G-004 第3節: 初期状態はオフ(削除しない)
        var vm = BuildViewModel();
        Assert.False(vm.DeleteImagesAfterPost);
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
    // 文字数表示(P-02, 04書 5.3節・G-002 第5節)
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
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<AppConfig>()))
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
    // 画像削除チェックボックスの有効/無効(04書 G-004 第4節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_画像フォルダ未設定の場合は削除チェックボックスが無効()
    {
        var vm = BuildViewModel();
        Assert.False(vm.IsDeleteCheckboxEnabled);
    }

    [Fact]
    public void 正常系_画像フォルダ設定時は削除チェックボックスが有効()
    {
        var vm = BuildViewModel();
        vm.ImageFolderPath = @"C:\images";
        Assert.True(vm.IsDeleteCheckboxEnabled);
    }

    [Fact]
    public void 正常系_ImageFolderPath変更時にIsDeleteCheckboxEnabledのPropertyChangedが発火する()
    {
        var vm = BuildViewModel();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.ImageFolderPath = @"C:\images";

        Assert.Contains(nameof(MainWindowViewModel.IsDeleteCheckboxEnabled), raised);
    }

    // ──────────────────────────────────────────────────────────────────
    // 画像フォルダのクリア(04書 G-003 第2節)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void 正常系_ClearImageFolderCommandで画像フォルダパスを未設定に戻す()
    {
        var vm = BuildViewModel();
        vm.ImageFolderPath = @"C:\images";

        vm.ClearImageFolderCommand.Execute(null);

        Assert.Null(vm.ImageFolderPath);
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
    public async Task 正常系_PostAsyncでApplication層に投稿内容と画像フォルダパスを渡す()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", @"C:\images", config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "hello";
        vm.ImageFolderPath = @"C:\images";

        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync("hello", @"C:\images", config), Times.Once);
    }

    [Fact]
    public async Task 正常系_画像フォルダ未設定時はnullを渡す()
    {
        var config = ValidConfig();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync("hello", null, config)).ReturnsAsync(0);
        var vm = BuildViewModel(app: mockApp, config: config);
        vm.Content = "hello";

        await vm.PostAsync();

        mockApp.Verify(x => x.RunAsync("hello", null, config), Times.Once);
    }

    [Fact]
    public async Task 正常系_投稿中はIsBusyがtrueになり完了後falseに戻る()
    {
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<AppConfig>()))
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
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<AppConfig>()))
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
    // クリップボード画像貼り付け(04書 G-010)
    // ──────────────────────────────────────────────────────────────────

    private static System.Windows.Media.Imaging.BitmapSource DummyImage() =>
        new System.Windows.Media.Imaging.WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

    [Fact]
    public void 正常系_画像を貼り付けると一時フォルダが画像フォルダパスに設定される()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1");
        var vm = BuildViewModel(clipboardImageStore: mockStore);

        vm.PasteImage(DummyImage());

        Assert.Equal(@"C:\temp\paste1", vm.ImageFolderPath);
    }

    [Fact]
    public void 正常系_再度貼り付けると前回の一時フォルダを削除する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.SetupSequence(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>()))
            .Returns(@"C:\temp\paste1")
            .Returns(@"C:\temp\paste2");
        var vm = BuildViewModel(clipboardImageStore: mockStore);

        vm.PasteImage(DummyImage());
        vm.PasteImage(DummyImage());

        Assert.Equal(@"C:\temp\paste2", vm.ImageFolderPath);
        mockStore.Verify(x => x.Delete(@"C:\temp\paste1"), Times.Once);
        mockStore.Verify(x => x.Delete(@"C:\temp\paste2"), Times.Never);
    }

    [Fact]
    public void 正常系_貼り付け後にClearImageFolderCommandを実行すると一時フォルダを削除する()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());

        vm.ClearImageFolderCommand.Execute(null);

        Assert.Null(vm.ImageFolderPath);
        mockStore.Verify(x => x.Delete(@"C:\temp\paste1"), Times.Once);
    }

    [Fact]
    public void 正常系_貼り付け後に画像フォルダを選択すると一時フォルダを削除する()
    {
        // 04書 G-003 第3.1節: フォルダ選択・クリップボード画像貼り付けは互いに排他
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1");
        var vm = BuildViewModel(clipboardImageStore: mockStore);
        vm.PasteImage(DummyImage());

        vm.ImageFolderPath = @"C:\real\folder";

        Assert.Equal(@"C:\real\folder", vm.ImageFolderPath);
        mockStore.Verify(x => x.Delete(@"C:\temp\paste1"), Times.Once);
    }

    [Fact]
    public void 正常系_貼り付けにより削除チェックボックスが有効になる()
    {
        var mockStore = new Mock<IClipboardImageStore>();
        mockStore.Setup(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>())).Returns(@"C:\temp\paste1");
        var vm = BuildViewModel(clipboardImageStore: mockStore);

        vm.PasteImage(DummyImage());

        Assert.True(vm.IsDeleteCheckboxEnabled);
    }

    [Fact]
    public async Task 正常系_投稿中は貼り付けを無視する()
    {
        // 04書 G-010 第6節
        var tcs = new TaskCompletionSource<int>();
        var mockApp = new Mock<IIchiPosApplication>();
        mockApp.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<AppConfig>()))
            .Returns(tcs.Task);
        var mockStore = new Mock<IClipboardImageStore>();
        var vm = BuildViewModel(app: mockApp, clipboardImageStore: mockStore);
        vm.Content = "hello";

        var postTask = vm.PostAsync();
        vm.PasteImage(DummyImage());

        mockStore.Verify(x => x.SaveToTempFolder(It.IsAny<System.Windows.Media.Imaging.BitmapSource>()), Times.Never);
        Assert.Null(vm.ImageFolderPath);

        tcs.SetResult(0);
        await postTask;
    }
}
