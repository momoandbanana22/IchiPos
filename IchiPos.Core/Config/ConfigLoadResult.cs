namespace IchiPos.Config;

public class ConfigLoadResult
{
    public bool IsSuccess { get; private set; }
    public AppConfig? Config { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ConfigLoadResult() { }

    public static ConfigLoadResult Success(AppConfig config)
    {
        return new ConfigLoadResult
        {
            IsSuccess = true,
            Config = config
        };
    }

    public static ConfigLoadResult Failure(string errorMessage)
    {
        return new ConfigLoadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
