namespace IchiPos.Output;

/// <summary>
/// 画像削除確認(F-011)のGUI版(04書 G-004)。
/// コンソールで対話的に確認する代わりに、投稿前に設定済みの状態(チェックボックス等)を参照して回答する。
/// </summary>
public class PresetUserPrompt : IUserPrompt
{
    private readonly Func<bool> _shouldDelete;

    public PresetUserPrompt(Func<bool> shouldDelete)
    {
        _shouldDelete = shouldDelete;
    }

    public string? Ask(string question) => _shouldDelete() ? "y" : "n";
}
