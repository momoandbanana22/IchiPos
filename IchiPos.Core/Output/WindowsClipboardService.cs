using System.Collections.Specialized;
using System.Windows.Forms;

namespace IchiPos.Output;

public class WindowsClipboardService : IClipboardService
{
    public void SetImages(IReadOnlyList<string> imagePaths)
    {
        // Windows クリップボード API は STA スレッドを要求するため専用スレッドで実行する。
        var thread = new Thread(() =>
        {
            var files = new StringCollection();
            files.AddRange(imagePaths.ToArray());
            Clipboard.SetFileDropList(files);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
