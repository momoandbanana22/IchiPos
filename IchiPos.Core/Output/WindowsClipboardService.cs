using System.Drawing;
using System.Windows.Forms;

namespace IchiPos.Output;

public class WindowsClipboardService : IClipboardService
{
    public void SetImage(string imagePath)
    {
        // Windows クリップボード API は STA スレッドを要求するため専用スレッドで実行する。
        var thread = new Thread(() =>
        {
            using var image = Image.FromFile(imagePath);
            Clipboard.SetImage(image);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
