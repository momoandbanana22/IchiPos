namespace IchiPos.Output;

public interface IOutputWriter
{
    void WriteSuccess(string message);
    void WriteError(string message);
    void WriteInfo(string message);
}

public class OutputWriter : IOutputWriter
{
    private readonly TextWriter _out;
    private readonly TextWriter _error;

    public OutputWriter() : this(Console.Out, Console.Error) { }

    public OutputWriter(TextWriter @out, TextWriter error)
    {
        _out = @out;
        _error = error;
    }

    public void WriteSuccess(string message) => _out.WriteLine(message);

    public void WriteError(string message) => _error.WriteLine(message);

    public void WriteInfo(string message) => _out.WriteLine(message);
}
