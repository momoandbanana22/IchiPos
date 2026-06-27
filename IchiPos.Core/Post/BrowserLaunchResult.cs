namespace IchiPos.Post;

public class BrowserLaunchResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    private BrowserLaunchResult() { }

    public static BrowserLaunchResult Success()
    {
        return new BrowserLaunchResult
        {
            IsSuccess = true
        };
    }

    public static BrowserLaunchResult Failure(string errorMessage)
    {
        return new BrowserLaunchResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
