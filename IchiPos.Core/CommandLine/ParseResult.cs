namespace IchiPos.CommandLine;

public class ParseResult
{
    public bool IsSuccess { get; private set; }
    public bool IsVersionRequest { get; private set; }
    public string? Content { get; private set; }
    public string? ImagePath { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// 添付画像のセンシティブフラグ（issue #107、01書「画像のセンシティブフラグ」節）。
    /// 既定は ON（true）。--sensitive off を指定したときのみ false になる。
    /// </summary>
    public bool IsSensitive { get; private set; } = true;

    private ParseResult() { }

    public static ParseResult Success(string content, string? imagePath, bool isSensitive = true)
    {
        return new ParseResult
        {
            IsSuccess = true,
            Content = content,
            ImagePath = imagePath,
            IsSensitive = isSensitive
        };
    }

    public static ParseResult Failure(string errorMessage)
    {
        return new ParseResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    public static ParseResult VersionRequest()
    {
        return new ParseResult { IsVersionRequest = true };
    }
}
