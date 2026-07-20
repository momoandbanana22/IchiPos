using System.Windows.Media.Imaging;

namespace IchiPos.Gui;

/// <summary>
/// クリップボード画像を一時ファイルへPNGとして保存するIClipboardImageStore実装。
/// 保存先は %TEMP%\IchiPos\&lt;GUID&gt;\pasted_&lt;GUID&gt;.png とし、貼り付けのたびに新しいフォルダ・ファイルを使う
/// (1貼り付け=1フォルダ=1ファイルのため、個別削除の単位としてそのままフォルダごと削除できる)。
/// ファイル名にもGUIDを含めるのは、複数の一時ファイルをX投稿画面へファイルドロップリストとして
/// 同時に渡す際(F-008第5項)に同名ファイルが重複とみなされないようにするため(04書 G-010第4.1節)。
/// </summary>
public class TempClipboardImageStore : IClipboardImageStore
{
    public string SaveToTempFile(BitmapSource image)
    {
        var id = Guid.NewGuid().ToString("N");
        var folder = Path.Combine(Path.GetTempPath(), "IchiPos", id);
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"pasted_{id}.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(stream);
        }

        return filePath;
    }

    public void Delete(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        if (folder != null && Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
