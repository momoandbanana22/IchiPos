namespace IchiPos.Validation;

public class PrePostValidationResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    private PrePostValidationResult() { }

    public static PrePostValidationResult Success()
    {
        return new PrePostValidationResult
        {
            IsSuccess = true
        };
    }

    public static PrePostValidationResult Failure(string errorMessage)
    {
        return new PrePostValidationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
