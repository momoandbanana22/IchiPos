using System.Windows.Forms;
using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

/// <summary>
/// F-008 第5項: 画像をファイルドロップリスト形式（CF_HDROP）でクリップボードへコピーすることを検証する。
///
/// テストは2層に分ける（#91）。
/// 1. <b>決定的テスト（どの環境でも毎回実行）</b>: 転送内容・枚数・順序・STA実行という「我々のロジック」を、
///    <see cref="FakeClipboard"/> / seam 経由で実クリップボードに依存せず検証する。
/// 2. <b>実クリップボード統合テスト（CI限定, <see cref="SkipIfNotCI"/>）</b>: CF_HDROP形式・STAスレッド破棄後の
///    保持という「OS/OLEの契約」は実クリップボードでしか検証できない。クリップボードはログインセッションで唯一の
///    共有資源のため、人間が同じPCでクリップボードを使う開発機では競合して間欠失敗する。人間が介在せず競合しない
///    CI（GitHub Actions）でのみ実行し、<see cref="ClipboardTestCollection"/> で並行実行も無効化する。
///    クリップボードを持たない環境（対話セッションのないランナー等）では <see cref="SkipIfClipboardUnavailable"/>
///    でスキップする。これらは release.yml の <c>dotnet test</c> でも実行される。
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

    /// <summary>
    /// 実クリップボード（ログインセッションで唯一の共有資源）を読み書きするテストは、人間が同じPCで
    /// クリップボードを使う開発機では他プロセス・他アプリと競合して間欠失敗する（#91）。CF_HDROP形式や
    /// STAスレッド破棄後の保持といったOS境界の挙動は我々のロジックでは代替検証できず実クリップボードを要するため、
    /// 人間が介在せず競合しないCI（GitHub Actions は環境変数 <c>CI</c> を設定する）でのみ実行する。ローカルでは
    /// スキップし（転送内容・順序・STA実行は決定的テストが毎回検証する）、CIが毎PR実境界を裏打ちする。
    /// これは失敗を握りつぶすスキップではなく、実検証をCIへ寄せる環境ゲートである。
    /// 手元で意図的に確認したい場合は環境変数 <c>CI=true</c> を設定して実行する。
    /// </summary>
    private static void SkipIfNotCI() =>
        Skip.If(
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")),
            "実クリップボード依存テストは競合のないCIでのみ実行する（#91）。手元で走らせるには環境変数 CI=true。");

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

    // ── 実クリップボード統合テスト（CI限定）──
    // OS/OLEの契約（実際にCF_HDROPで載る・STAスレッド破棄後も保持される）は実クリップボードでしか
    // 検証できない。開発機では人間とクリップボードを奪い合い間欠失敗するため、CIでのみ実行する（#91）。

    [SkippableFact]
    public void 正常系_ファイルドロップリスト形式でクリップボードに載る()
    {
        // CF_HDROP形式で実際に載ることの検証。この「OSがどの形式で保持するか」は実クリップボードを要する。
        SkipIfNotCI();
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(2);

        new WindowsClipboardService().SetImages(paths);

        var formats = OnStaThread(() => Clipboard.GetDataObject()?.GetFormats() ?? []);
        Assert.Contains(DataFormats.FileDrop, formats);
    }

    [SkippableFact]
    public void 正常系_コピーに使ったSTAスレッドの終了後もクリップボードの内容が保持される()
    {
        // SetImages は専用STAスレッドを生成しJoin後に破棄する。スレッド破棄によってクリップボードの内容が
        // 失われないこと（失われるとX下書き画面に貼り付けられない）を担保する。破棄後の保持はOLEのフラッシュ
        // 挙動に依存し、我々のスレッド運用がそれを壊していないかは実クリップボードでしか検証できない。
        SkipIfNotCI();
        SkipIfClipboardUnavailable();
        var paths = CreateImageFiles(4);

        new WindowsClipboardService().SetImages(paths);

        // 別のSTAスレッドから読み直しても4件残っていること。
        var actual = ReadFileDropList();
        Assert.Equal(4, actual.Count);
        Assert.Equal(paths, actual);
    }

    // ── 決定的テスト（実クリップボード不要・どの環境でも毎回実行）──
    // 我々が実装したロジック（転送内容・枚数・順序・STA実行・リトライと失敗の表面化）を、seam / FakeClipboard で
    // 決定的に検証する。共有資源に依存しないため間欠失敗しない。

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void SetImages_渡したパスをそのままの順序で下位設定へ転送する(int count)
    {
        // 転送内容と枚数の保証（実際にCF_HDROPとして載ることは上のCI限定テストが担う）。
        var fake = new FakeClipboard();
        var paths = Enumerable.Range(1, count).Select(i => $@"C:\img{i}.png").ToList();
        var service = new WindowsClipboardService(fake.Set, fake.Get, maxAttempts: 1, retryDelayMs: 0);

        service.SetImages(paths);

        Assert.Equal(paths, fake.Stored);
    }

    [Fact]
    public void SetImages_逆順で渡しても順序を保って転送する()
    {
        // 添付画像一覧の並べ替え（04書 G-011第5節）の結果がX側の添付順に反映されるよう、順序保持を担保する。
        var fake = new FakeClipboard();
        var paths = new List<string> { @"C:\c.png", @"C:\b.png", @"C:\a.png" };
        var service = new WindowsClipboardService(fake.Set, fake.Get, maxAttempts: 1, retryDelayMs: 0);

        service.SetImages(paths);

        Assert.Equal(paths, fake.Stored);
    }

    [Fact]
    public void SetImages_設定はSTAスレッド上で実行される()
    {
        // クリップボードAPIはSTAを要求する。SetImagesが下位設定を必ずSTAスレッドで実行することを、
        // 実クリップボードに依存せず決定的に検証する。
        ApartmentState? apartmentAtSet = null;
        var store = new List<string>();
        var paths = new List<string> { @"C:\a.png" };
        var service = new WindowsClipboardService(
            set: p => { apartmentAtSet = Thread.CurrentThread.GetApartmentState(); store = p.ToList(); },
            get: () => store,
            maxAttempts: 1,
            retryDelayMs: 0);

        service.SetImages(paths);

        Assert.Equal(ApartmentState.STA, apartmentAtSet);
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
}
