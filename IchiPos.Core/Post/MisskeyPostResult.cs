namespace IchiPos.Post;

public class MisskeyPostResult
{
    public bool IsSuccess { get; private set; }
    public string? NoteId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private MisskeyPostResult() { }

    public static MisskeyPostResult Success(string noteId)
    {
        return new MisskeyPostResult
        {
            IsSuccess = true,
            NoteId = noteId
        };
    }

    public static MisskeyPostResult Failure(string errorMessage)
    {
        return new MisskeyPostResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
