using System.Windows.Media.Imaging;

namespace IchiPos.Gui;

/// <summary>クリップボード画像貼り付け機能(04書 G-010)の画像永続化。</summary>
public interface IClipboardImageStore
{
    /// <summary>画像を一時フォルダへ保存し、そのフォルダパスを返す。</summary>
    string SaveToTempFolder(BitmapSource image);

    /// <summary>指定フォルダを削除する。</summary>
    void Delete(string folderPath);
}
