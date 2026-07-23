using System.Collections.Specialized;
using System.Windows.Forms;

namespace IchiPos.Output;

public class WindowsClipboardService : IClipboardService
{
    private readonly Action<IReadOnlyList<string>> _set;
    private readonly Func<IReadOnlyList<string>> _get;
    private readonly int _maxAttempts;
    private readonly int _retryDelayMs;

    public WindowsClipboardService()
        : this(DefaultSet, DefaultGet, maxAttempts: 10, retryDelayMs: 50) { }

    /// <summary>
    /// テスト用。実クリップボードを使わずに設定・読み戻し・リトライ挙動を検証するため、
    /// 設定アクションと読み戻しアクション、試行回数・間隔を差し替えられるようにする。
    /// </summary>
    public WindowsClipboardService(
        Action<IReadOnlyList<string>> set,
        Func<IReadOnlyList<string>> get,
        int maxAttempts,
        int retryDelayMs)
    {
        _set = set;
        _get = get;
        _maxAttempts = maxAttempts;
        _retryDelayMs = retryDelayMs;
    }

    public void SetImages(IReadOnlyList<string> imagePaths)
    {
        // Windows クリップボード API は STA スレッドを要求するため専用スレッドで実行する。
        // スレッド内の例外は握りつぶさず、Join 後に呼び出し側へ再送出する(issue #56)。
        RunOnStaThread(() => SetWithRetry(imagePaths));
    }

    /// <summary>
    /// クリップボードはプロセス横断の単一所有リソースで、他プロセスとの競合により設定が
    /// 一時的に失敗する(例外・あるいは設定しても載らず読み戻しが空になる)。設定後に読み戻して
    /// 検証し、載るまで数回リトライする。最終的に載らなければ <see cref="ClipboardCopyException"/>
    /// を投げて失敗を表面化する(issue #56)。
    /// </summary>
    private void SetWithRetry(IReadOnlyList<string> imagePaths)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                _set(imagePaths);
                if (_get().SequenceEqual(imagePaths)) return;
            }
            catch (Exception ex)
            {
                lastError = ex; // 一時的な失敗とみなしてリトライする
            }

            if (attempt < _maxAttempts && _retryDelayMs > 0) Thread.Sleep(_retryDelayMs);
        }

        throw new ClipboardCopyException(
            $"画像のクリップボードへのコピーに失敗しました（{_maxAttempts}回試行）。",
            lastError ?? new InvalidOperationException("設定後の読み戻しが一致しませんでした。"));
    }

    private static void DefaultSet(IReadOnlyList<string> imagePaths)
    {
        var files = new StringCollection();
        files.AddRange(imagePaths.ToArray());
        Clipboard.SetFileDropList(files);
    }

    private static IReadOnlyList<string> DefaultGet() =>
        Clipboard.GetFileDropList().Cast<string>().ToList();

    private static void RunOnStaThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw error;
    }
}
