namespace IchiPos.Post;

public class Mixi2MediaUploadResult
{
    public bool IsSuccess { get; private set; }
    public string? MediaId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Mixi2MediaUploadResult() { }

    public static Mixi2MediaUploadResult Success(string mediaId)
    {
        return new Mixi2MediaUploadResult
        {
            IsSuccess = true,
            MediaId = mediaId
        };
    }

    public static Mixi2MediaUploadResult Failure(string errorMessage)
    {
        return new Mixi2MediaUploadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
