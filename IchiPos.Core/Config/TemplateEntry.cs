namespace IchiPos.Config;

/// <summary>
/// 定型文1件（04書 G-016、issue #111）。Misskey用とX用の本文を持つ。
/// 設定に文字列だけで登録された場合は両者に同じ本文を用いる（後方互換）。
/// マップ（misskey / x）で片方を省略した場合は、もう片方の本文を流用する（<see cref="TemplateEntryYamlConverter"/>）。
/// </summary>
public sealed class TemplateEntry
{
    public TemplateEntry(string misskeyText, string xText)
    {
        MisskeyText = misskeyText;
        XText = xText;
    }

    /// <summary>Misskeyへ投稿する本文。</summary>
    public string MisskeyText { get; }

    /// <summary>X投稿画面（下書き）へ渡す本文。</summary>
    public string XText { get; }

    /// <summary>
    /// Misskey本文とX本文が異なるか。GUIはtrueのとき、押下前に投稿先を確認できるよう
    /// 両本文をラベル付きで表示する（04書 G-016 第3節）。
    /// </summary>
    public bool IsSplit => !string.Equals(MisskeyText, XText, StringComparison.Ordinal);
}
