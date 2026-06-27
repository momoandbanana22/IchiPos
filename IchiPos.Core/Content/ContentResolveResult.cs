namespace IchiPos.Content;

public class ContentResolveResult
{
    public bool IsSuccess { get; private set; }
    public string? Content { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ContentResolveResult() { }

    public static ContentResolveResult Success(string content)
    {
        return new ContentResolveResult
        {
            IsSuccess = true,
            Content = content
        };
    }

    public static ContentResolveResult Failure(string errorMessage)
    {
        return new ContentResolveResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
