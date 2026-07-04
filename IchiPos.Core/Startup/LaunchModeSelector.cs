namespace IchiPos.Startup;

public interface ILaunchModeSelector
{
    LaunchMode Determine(string[] args);
}

public class LaunchModeSelector : ILaunchModeSelector
{
    public LaunchMode Determine(string[] args)
    {
        return args.Length == 0 ? LaunchMode.Gui : LaunchMode.Cli;
    }
}
