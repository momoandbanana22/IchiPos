namespace IchiPos.Output;

/// <summary>
/// クリップボードへのコピーが最終的に失敗したことを表す例外(issue #56)。
/// クリップボードはプロセス横断の単一所有リソースで一時的に失敗しうるため、
/// <see cref="WindowsClipboardService"/> はリトライしても載らなかった場合にこれを投げる
/// (失敗を握りつぶさず呼び出し側に表面化する)。
/// </summary>
public class ClipboardCopyException : Exception
{
    public ClipboardCopyException(string message) : base(message) { }

    public ClipboardCopyException(string message, Exception innerException)
        : base(message, innerException) { }
}
