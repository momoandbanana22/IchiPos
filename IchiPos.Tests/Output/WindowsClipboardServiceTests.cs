using System.Windows.Forms;
using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

/// <summary>
/// F-008 第5項: 画像をファイルドロップリスト形式（CF_HDROP）でクリップボードへコピーすることを検証する。
///
/// 実際のWindowsクリップボードを読み書きするため、以下2点に配慮している。
/// 1. クリップボードはプロセス横断の共有資源のため、<see cref="ClipboardTestCollection"/> で並行実行を無効化する。
/// 2. クリップボードを持たない環境（対話セッションのないCIランナー等）では実行できないため、
///    失敗ではなくスキップとして扱う。これらのテストは release.yml の <c>dotnet test</c> でも実行され、
///    ここで失敗するとリリース自体が中断されるため、環境要因でリリースを壊さないようにする。
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class WindowsClipboardServiceTests : IDisposable
{
    private readonly string _workDir;

    public WindowsClipboardServiceTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "IchiPos.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    /// <summary>クリップボードAPIはSTAスレッドを要求するため、専用STAスレッドで実行する。</summary>
    private static T OnStaThread<T>(Func<T> action)
    {
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { result = action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw error;
        return result;
    }

    /// <summary>
    /// クリップボードが利用できない環境ではテストをスキップする。
    /// 利用可能かどうかは実際に読み取りを試みて判定する（対話セッションがない場合ここで例外になる）。
    /// </summary>
    private static void SkipIfClipboardUnavailable()
    {
        Exception? error = null;
        try
        {
            OnStaThread<object?>(() => Clipboard.GetDataObject());
        }
        catch (Exception ex)
        {
            error = ex;
        }
        Skip.If(error != null, $"このテストは利用可能なクリップボードを必要とする: {error?.Message}");
    }

    private static List<string> ReadFileDropList() =>
        OnStaThread(() => Clipboard.GetFileDropList().Cast<string>().ToList());

    private List<string> CreateImageFiles(int count)
    {
        var paths = new List<string>();
        for (var i = 1; i <= count; i++)
        {
            var path = Path.Combine(_workDir, $"img{i}.png");
            File.WriteAllBytes(path, [0x89, 0x50, 0x4E, 0x47]);
            paths.Add(path);
        }
        return paths;
    }

    [SkippableFact]
    public void 正常系_4枚のパスがファイルドロップリストとしてクリップボードに載る()
    {
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(4);

        new WindowsClipboardService().SetImages(paths);

        Assert.Equal(paths, ReadFileDropList());
    }

    [SkippableFact]
    public void 正常系_ファイルドロップリスト形式でクリップボードに載る()
    {
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(2);

        new WindowsClipboardService().SetImages(paths);

        var formats = OnStaThread(() => Clipboard.GetDataObject()?.GetFormats() ?? []);
        Assert.Contains(DataFormats.FileDrop, formats);
    }

    [SkippableFact]
    public void 正常系_1枚でもファイルドロップリストとして載る()
    {
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(1);

        new WindowsClipboardService().SetImages(paths);

        Assert.Equal(paths, ReadFileDropList());
    }

    [SkippableFact]
    public void 正常系_コピーに使ったSTAスレッドの終了後もクリップボードの内容が保持される()
    {
        // SetImages は専用STAスレッドを生成しJoin後に破棄する。スレッド破棄によって
        // クリップボードの内容が失われないことを担保する（失われるとX下書き画面に貼り付けられない）。
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(4);

        new WindowsClipboardService().SetImages(paths);

        // 別のSTAスレッドから読み直しても4件残っていること。
        var actual = ReadFileDropList();
        Assert.Equal(4, actual.Count);
        Assert.Equal(paths, actual);
    }

    // ── issue #56: リトライと失敗の表面化（実クリップボード不要の決定的テスト） ──

    [Fact]
    public void SetImages_設定が数回サイレントに失敗しても_リトライして最終的に載る()
    {
        // 設定はしたが載らない（読み戻しが空）状態が続いても、読み戻し検証で気づいてリトライする。
        var fake = new FakeClipboard { SilentFailsBeforeSuccess = 2 };
        var paths = new List<string> { @"C:\a.png", @"C:\b.png" };
        var service = new WindowsClipboardService(fake.Set, fake.Get, maxAttempts: 5, retryDelayMs: 0);

        service.SetImages(paths);

        Assert.Equal(paths, fake.Stored);
        Assert.Equal(3, fake.SetCallCount); // 2回サイレント失敗 + 3回目で成功
    }

    [Fact]
    public void SetImages_設定が例外を投げても_リトライして最終的に載る()
    {
        // クリップボードのオープン失敗（CLIPBRD_E_CANT_OPEN相当）で例外が飛んでもリトライする。
        var fake = new FakeClipboard { ThrowsBeforeSuccess = 2 };
        var paths = new List<string> { @"C:\a.png" };
        var service = new WindowsClipboardService(fake.Set, fake.Get, maxAttempts: 5, retryDelayMs: 0);

        service.SetImages(paths);

        Assert.Equal(paths, fake.Stored);
    }

    [Fact]
    public void SetImages_全試行で失敗する場合_握りつぶさず例外を投げる()
    {
        // 修正前は失敗を握りつぶして正常終了していた。全リトライ失敗時は例外で表面化する。
        var fake = new FakeClipboard { AlwaysSilentFail = true };
        var paths = new List<string> { @"C:\a.png" };
        var service = new WindowsClipboardService(fake.Set, fake.Get, maxAttempts: 3, retryDelayMs: 0);

        Assert.Throws<ClipboardCopyException>(() => service.SetImages(paths));
        Assert.Equal(3, fake.SetCallCount); // maxAttempts 回試行した
    }

    /// <summary>実クリップボードを使わず、設定の失敗・リトライ挙動を決定的に再現するための偽クリップボード。</summary>
    private sealed class FakeClipboard
    {
        private List<string> _stored = new();

        public int SetCallCount { get; private set; }

        /// <summary>成功するまでにサイレントに失敗する（設定しても載らない）回数。</summary>
        public int SilentFailsBeforeSuccess { get; set; }

        /// <summary>成功するまでに例外を投げる回数。</summary>
        public int ThrowsBeforeSuccess { get; set; }

        /// <summary>常にサイレント失敗する（載らない）。</summary>
        public bool AlwaysSilentFail { get; set; }

        public IReadOnlyList<string> Stored => _stored;

        public void Set(IReadOnlyList<string> paths)
        {
            SetCallCount++;
            if (AlwaysSilentFail) return;
            if (SetCallCount <= ThrowsBeforeSuccess) throw new InvalidOperationException("クリップボードを開けません");
            if (SetCallCount <= ThrowsBeforeSuccess + SilentFailsBeforeSuccess) return;
            _stored = paths.ToList();
        }

        public IReadOnlyList<string> Get() => _stored;
    }

    [SkippableFact]
    public void 正常系_渡した順序どおりにクリップボードへ載る()
    {
        // 添付画像一覧の並べ替え（04書 G-011第5節）の結果がX側の添付順に反映されるようにするため、
        // 順序が保たれることを担保する。
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(4);
        paths.Reverse();

        new WindowsClipboardService().SetImages(paths);

        Assert.Equal(paths, ReadFileDropList());
    }
}
