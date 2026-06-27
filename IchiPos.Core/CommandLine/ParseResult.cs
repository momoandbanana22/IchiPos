namespace IchiPos.CommandLine;

public class ParseResult
{
    public bool IsSuccess { get; private set; }
    public string? Content { get; private set; }
    public string? ImagePath { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ParseResult() { }

    public static ParseResult Success(string content, string? imagePath)
    {
        return new ParseResult
        {
            IsSuccess = true,
            Content = content,
            ImagePath = imagePath
        };
    }

    public static ParseResult Failure(string errorMessage)
    {
        return new ParseResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
