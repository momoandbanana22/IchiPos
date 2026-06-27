namespace IchiPos.Output;

public class OutputWriter
{
    public void WriteSuccess(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteError(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }
}
