namespace IchiPos.Validation;

public class PrePostValidator
{
    public PrePostValidationResult Validate(string content, List<string> imagePaths, int maxLength)
    {
        // 投稿テキストが空かチェック
        if (string.IsNullOrWhiteSpace(content))
        {
            return PrePostValidationResult.Failure("投稿テキストが空です");
        }

        // 投稿テキストの長さチェック
        if (content.Length > maxLength)
        {
            return PrePostValidationResult.Failure($"投稿テキストが長さ制限を超えています（{maxLength}文字以内）");
        }

        return PrePostValidationResult.Success();
    }
}
