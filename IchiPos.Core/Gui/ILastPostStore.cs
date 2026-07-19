namespace IchiPos.Gui;

/// <summary>
/// 前回投稿内容の記録(04書 G-015第4節)。投稿本文そのものではなく、比較用テキストのハッシュ値のみを保持する。
/// 読み書きの失敗は投稿処理を妨げてはならないため、実装は例外を投げずに握りつぶすこと。
/// </summary>
public interface ILastPostStore
{
    /// <summary>記録されている前回投稿内容のハッシュ値を返す。記録がない・読み出しに失敗した場合はnull。</summary>
    string? LoadHash();

    /// <summary>前回投稿内容のハッシュ値を上書き保存する(履歴は保持しない)。</summary>
    void SaveHash(string hash);
}
