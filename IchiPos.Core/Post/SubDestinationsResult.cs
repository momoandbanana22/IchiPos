namespace IchiPos.Post;

public class SubDestinationsResult
{
    public Mixi2PostResult Mixi2 { get; }
    public XPostLaunchResult X { get; }

    public SubDestinationsResult(Mixi2PostResult mixi2, XPostLaunchResult x)
    {
        Mixi2 = mixi2;
        X = x;
    }
}
