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
