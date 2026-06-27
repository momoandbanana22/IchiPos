using System.Diagnostics;

namespace IchiPos.Post;

public class SystemProcessStarter : IProcessStarter
{
    public bool Start(string url)
    {
        var psi = new ProcessStartInfo(url) { UseShellExecute = true };
        return Process.Start(psi) != null;
    }
}
