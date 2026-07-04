namespace IchiPos.Gui;

/// <summary>
/// 投稿添付画像候補1件(サムネイル一覧の1項目、04書 G-013)。
/// IsTemporaryは貼り付け由来(クリップボード保存の一時ファイル)かどうかを表し、
/// trueの場合のみリストからの除去時に実ファイルを削除する(所有権あり)。
/// フォルダ選択・複数ファイル貼り付け由来(false)はユーザーの実ファイルへの参照のみで削除しない。
/// </summary>
public class AttachedImage
{
    public AttachedImage(string filePath, bool isTemporary)
    {
        FilePath = filePath;
        IsTemporary = isTemporary;
    }

    public string FilePath { get; }
    public bool IsTemporary { get; }
}
