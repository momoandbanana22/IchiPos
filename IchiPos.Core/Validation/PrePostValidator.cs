using System.Drawing;

namespace IchiPos.Validation;

public interface IPrePostValidator
{
    PrePostValidationResult Validate(string content, List<string> imagePaths, int maxLength);
}

public class PrePostValidator : IPrePostValidator
{
    private readonly Func<string, bool> _isImageReadable;

    public PrePostValidator(Func<string, bool>? isImageReadable = null)
    {
        _isImageReadable = isImageReadable ?? TryLoadImage;
    }

    public PrePostValidationResult Validate(string content, List<string> imagePaths, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return PrePostValidationResult.Failure("投稿テキストが空です");

        if (content.Length > maxLength)
            return PrePostValidationResult.Failure($"投稿テキストが長さ制限を超えています（{maxLength}文字以内）");

        foreach (var path in imagePaths)
        {
            if (!_isImageReadable(path))
                return PrePostValidationResult.Failure($"画像ファイルが読み込めません: {path}");
        }

        return PrePostValidationResult.Success();
    }

    private static bool TryLoadImage(string path)
    {
        try
        {
            using var image = Image.FromFile(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
