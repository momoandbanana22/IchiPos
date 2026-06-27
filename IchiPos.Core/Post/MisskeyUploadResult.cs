namespace IchiPos.Post;

public class MisskeyUploadResult
{
    public bool IsSuccess { get; private set; }
    public string? FileId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private MisskeyUploadResult() { }

    public static MisskeyUploadResult Success(string fileId)
    {
        return new MisskeyUploadResult
        {
            IsSuccess = true,
            FileId = fileId
        };
    }

    public static MisskeyUploadResult Failure(string errorMessage)
    {
        return new MisskeyUploadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
