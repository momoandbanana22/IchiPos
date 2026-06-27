namespace IchiPos.Content;

public class TextFileReadResult
{
    public bool IsSuccess { get; private set; }
    public string? Content { get; private set; }
    public string? ErrorMessage { get; private set; }

    private TextFileReadResult() { }

    public static TextFileReadResult Success(string content)
    {
        return new TextFileReadResult
        {
            IsSuccess = true,
            Content = content
        };
    }

    public static TextFileReadResult Failure(string errorMessage)
    {
        return new TextFileReadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
