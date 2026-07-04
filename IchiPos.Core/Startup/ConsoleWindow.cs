using System.Runtime.InteropServices;

namespace IchiPos.Startup;

/// <summary>GUIモード起動時にコンソールウィンドウを非表示にする(04書 4.1節・9.2節)。</summary>
public static class ConsoleWindow
{
    private const int SW_HIDE = 0;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void Hide()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }
}
