namespace IchiPos.Images;

public class ImageValidationResult
{
    public bool IsSuccess { get; private set; }
    public List<string> ValidImagePaths { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    private ImageValidationResult() { }

    public static ImageValidationResult Success(List<string> validImagePaths)
    {
        return new ImageValidationResult
        {
            IsSuccess = true,
            ValidImagePaths = validImagePaths
        };
    }

    public static ImageValidationResult Failure(string errorMessage)
    {
        return new ImageValidationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
