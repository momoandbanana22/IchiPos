namespace IchiPos.Output;

public interface IOutputWriter
{
    void WriteSuccess(string message);
    void WriteError(string message);
    void WriteInfo(string message);
}

public class OutputWriter : IOutputWriter
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
