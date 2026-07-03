namespace IchiPos.Content;

public interface IContentResolver
{
    Task<ContentResolveResult> ResolveAsync(string content);
}

public class ContentResolver : IContentResolver
{
    private readonly ITextFileReader _textFileReader;
    private readonly IDatePlaceholderReplacer _datePlaceholderReplacer;

    public ContentResolver(ITextFileReader textFileReader, IDatePlaceholderReplacer datePlaceholderReplacer)
    {
        _textFileReader = textFileReader;
        _datePlaceholderReplacer = datePlaceholderReplacer;
    }

    public async Task<ContentResolveResult> ResolveAsync(string content)
    {
        // .txt拡張子の場合
        if (content.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            // ファイルが存在しない場合はエラー
            if (!File.Exists(content))
            {
                return ContentResolveResult.Failure("ファイルが存在しません");
            }

            var result = await _textFileReader.ReadAsync(content);
            if (result.IsSuccess)
            {
                return ContentResolveResult.Success(_datePlaceholderReplacer.Replace(result.Content!));
            }
            else
            {
                return ContentResolveResult.Failure(result.ErrorMessage!);
            }
        }

        // 文字列として扱う
        return ContentResolveResult.Success(_datePlaceholderReplacer.Replace(content));
    }
}
