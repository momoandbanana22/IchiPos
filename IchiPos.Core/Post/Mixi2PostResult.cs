namespace IchiPos.Post;

public class Mixi2PostResult
{
    public bool IsSuccess { get; private set; }
    public bool IsSkipped { get; private set; }
    public string? PostId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Mixi2PostResult() { }

    public static Mixi2PostResult Success(string postId)
    {
        return new Mixi2PostResult
        {
            IsSuccess = true,
            PostId = postId
        };
    }

    public static Mixi2PostResult Skipped()
    {
        return new Mixi2PostResult
        {
            IsSkipped = true
        };
    }

    public static Mixi2PostResult Failure(string errorMessage)
    {
        return new Mixi2PostResult
        {
            ErrorMessage = errorMessage
        };
    }
}
