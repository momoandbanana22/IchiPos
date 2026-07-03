namespace IchiPos.Post;

public enum Mixi2MediaStatus
{
    UploadPending,
    Processing,
    Completed,
    Failed
}

public class Mixi2MediaStatusResult
{
    public bool IsSuccess { get; private set; }
    public Mixi2MediaStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Mixi2MediaStatusResult() { }

    public static Mixi2MediaStatusResult Success(Mixi2MediaStatus status)
    {
        return new Mixi2MediaStatusResult
        {
            IsSuccess = true,
            Status = status
        };
    }

    public static Mixi2MediaStatusResult Failure(string errorMessage)
    {
        return new Mixi2MediaStatusResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
