using System.Windows.Media.Imaging;

namespace IchiPos.Gui;

/// <summary>クリップボード画像貼り付け機能(04書 G-010)の画像永続化。</summary>
public interface IClipboardImageStore
{
    /// <summary>画像を一時ファイルへ保存し、そのファイルパスを返す。</summary>
    string SaveToTempFile(BitmapSource image);

    /// <summary>指定ファイル(とその格納用一時フォルダ)を削除する。</summary>
    void Delete(string filePath);
}
