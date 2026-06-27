using System.Diagnostics;

namespace IchiPos.Post;

public class SystemProcessStarter : IProcessStarter
{
    private readonly Func<ProcessStartInfo, Process?> _processStart;

    public SystemProcessStarter() : this(Process.Start) { }

    public SystemProcessStarter(Func<ProcessStartInfo, Process?> processStart)
    {
        _processStart = processStart;
    }

    public bool Start(string url)
    {
        var psi = new ProcessStartInfo(url) { UseShellExecute = true };
        // UseShellExecute=true でURLを起動するとシェルがブラウザへ委譲し
        // Process.Start は null を返すことがある（正常動作）。
        // 例外が発生しなければ成功として扱う。
        _processStart(psi);
        return true;
    }
}
