namespace IchiPos.Post;

public class XPostLaunchResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    private XPostLaunchResult() { }

    public static XPostLaunchResult Success()
    {
        return new XPostLaunchResult
        {
            IsSuccess = true
        };
    }

    public static XPostLaunchResult Failure(string errorMessage)
    {
        return new XPostLaunchResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
