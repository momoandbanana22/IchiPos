using System.Windows.Media.Imaging;

namespace IchiPos.Gui;

/// <summary>
/// クリップボード画像を一時フォルダへPNGとして保存するIClipboardImageStore実装。
/// 保存先は %TEMP%\IchiPos\&lt;GUID&gt;\pasted.png とし、貼り付けのたびに新しいフォルダを使う。
/// </summary>
public class TempClipboardImageStore : IClipboardImageStore
{
    public string SaveToTempFolder(BitmapSource image)
    {
        var folder = Path.Combine(Path.GetTempPath(), "IchiPos", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, "pasted.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(stream);
        }

        return folder;
    }

    public void Delete(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive: true);
        }
    }
}
