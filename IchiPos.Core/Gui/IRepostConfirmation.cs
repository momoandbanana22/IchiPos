namespace IchiPos.Gui;

/// <summary>同一内容の再投稿確認(04書 G-015第3節)。</summary>
public interface IRepostConfirmation
{
    /// <summary>再投稿してよいかユーザーに確認する。投稿を続行してよい場合はtrue、中止する場合はfalse。</summary>
    bool ConfirmRepost();
}
