using System.Windows.Forms;
using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

/// <summary>
/// F-008 第5項: 画像をファイルドロップリスト形式（CF_HDROP）でクリップボードへコピーすることを検証する。
/// 実際のWindowsクリップボードを書き換えるため、他のクリップボードテストと並行実行しない
/// （<see cref="ClipboardTestCollection"/>）。
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

    /// <summary>クリップボードAPIはSTAスレッドを要求するため、読み取りも専用STAスレッドで行う。</summary>
    private static T ReadClipboard<T>(Func<T> read)
    {
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { result = read(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw error;
        return result;
    }

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

    [Fact]
    public void 正常系_4枚のパスがファイルドロップリストとしてクリップボードに載る()
    {
        var paths = CreateImageFiles(4);

        new WindowsClipboardService().SetImages(paths);

        var actual = ReadClipboard(() => Clipboard.GetFileDropList().Cast<string>().ToList());
        Assert.Equal(paths, actual);
    }

    [Fact]
    public void 正常系_ファイルドロップリスト形式でクリップボードに載る()
    {
        var paths = CreateImageFiles(2);

        new WindowsClipboardService().SetImages(paths);

        var formats = ReadClipboard(() => Clipboard.GetDataObject()?.GetFormats() ?? []);
        Assert.Contains(DataFormats.FileDrop, formats);
    }

    [Fact]
    public void 正常系_1枚でもファイルドロップリストとして載る()
    {
        var paths = CreateImageFiles(1);

        new WindowsClipboardService().SetImages(paths);

        var actual = ReadClipboard(() => Clipboard.GetFileDropList().Cast<string>().ToList());
        Assert.Equal(paths, actual);
    }

    [Fact]
    public void 正常系_コピーに使ったSTAスレッドの終了後もクリップボードの内容が保持される()
    {
        // SetImages は専用STAスレッドを生成しJoin後に破棄する。スレッド破棄によって
        // クリップボードの内容が失われないことを担保する（失われるとX下書き画面に貼り付けられない）。
        var paths = CreateImageFiles(4);

        new WindowsClipboardService().SetImages(paths);

        // 別のSTAスレッドから読み直しても4件残っていること。
        var actual = ReadClipboard(() => Clipboard.GetFileDropList().Cast<string>().ToList());
        Assert.Equal(4, actual.Count);
        Assert.Equal(paths, actual);
    }

    [Fact]
    public void 正常系_渡した順序どおりにクリップボードへ載る()
    {
        // 添付画像一覧の並べ替え（04書 G-011第5節）の結果がX側の添付順に反映されるようにするため、
        // 順序が保たれることを担保する。
        var paths = CreateImageFiles(4);
        paths.Reverse();

        new WindowsClipboardService().SetImages(paths);

        var actual = ReadClipboard(() => Clipboard.GetFileDropList().Cast<string>().ToList());
        Assert.Equal(paths, actual);
    }
}
