using System.Runtime.InteropServices;

namespace IchiPos.Startup;

/// <summary>
/// 実行ファイル(GUIサブシステム)からコンソールを確保するWin32実装。
/// 呼び出し元が標準出力を既にリダイレクトしている場合(パイプ・ファイル等)は、
/// それを尊重してコンソールの割り当て・アタッチを一切行わない。
/// リダイレクトされていない場合のみ、呼び出し元のコンソールへのアタッチ、
/// それも失敗した場合は新規コンソールの割り当てを行う。
/// </summary>
public class Win32ConsoleAttacher : IConsoleAttacher
{
    private const int AttachParentProcess = -1;
    private const int StdOutputHandle = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    public bool AttachToParent()
    {
        if (HasRedirectedStandardOutput())
        {
            // 呼び出し元が標準出力をリダイレクト済み。.NETが起動時に設定済みのため何もしない。
            return true;
        }

        if (!AttachConsole(AttachParentProcess))
        {
            return false;
        }

        RebindStandardStreams();
        return true;
    }

    public void AllocateNew()
    {
        AllocConsole();
        RebindStandardStreams();
    }

    private static bool HasRedirectedStandardOutput()
    {
        var handle = GetStdHandle(StdOutputHandle);
        return handle != IntPtr.Zero && handle != new IntPtr(-1);
    }

    private static void RebindStandardStreams()
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
    }
}
