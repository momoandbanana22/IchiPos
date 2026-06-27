namespace IchiPos.Images;

public class ImageFolderReadResult
{
    public bool IsSuccess { get; private set; }
    public List<string> ImageFiles { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    private ImageFolderReadResult() { }

    public static ImageFolderReadResult Success(List<string> imageFiles)
    {
        return new ImageFolderReadResult
        {
            IsSuccess = true,
            ImageFiles = imageFiles
        };
    }

    public static ImageFolderReadResult Failure(string errorMessage)
    {
        return new ImageFolderReadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
