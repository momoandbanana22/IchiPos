namespace IchiPos.Content;

public class ContentResolver
{
    private readonly ITextFileReader _textFileReader;

    public ContentResolver(ITextFileReader textFileReader)
    {
        _textFileReader = textFileReader;
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
                return ContentResolveResult.Success(result.Content!);
            }
            else
            {
                return ContentResolveResult.Failure(result.ErrorMessage!);
            }
        }

        // 文字列として扱う
        return ContentResolveResult.Success(content);
    }
}
