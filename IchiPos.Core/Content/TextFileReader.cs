using System.Text;

namespace IchiPos.Content;

public interface ITextFileReader
{
    Task<TextFileReadResult> ReadAsync(string filePath);
}

public class TextFileReader : ITextFileReader
{
    static TextFileReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<TextFileReadResult> ReadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return TextFileReadResult.Failure("ファイルが存在しません");
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            var (encoding, bomLength) = DetectEncoding(bytes);

            if (encoding == null)
            {
                return TextFileReadResult.Failure("対応外のエンコードです");
            }

            var contentBytes = bomLength > 0 ? bytes[bomLength..] : bytes;
            var content = encoding.GetString(contentBytes);
            content = RemoveTrailingEolMarker(content);
            return TextFileReadResult.Success(content);
        }
        catch (Exception ex)
        {
            return TextFileReadResult.Failure($"ファイルの読み込みに失敗しました: {ex.Message}");
        }
    }

    private static string RemoveTrailingEolMarker(string content)
    {
        // テキストファイルの終端記号（EOF改行）は本文ではないため1つだけ除去する。
        // ユーザーが意図して追加した空行やスペースはそのまま残す。
        if (content.EndsWith("\r\n"))
        {
            return content[..^2];
        }
        if (content.EndsWith("\n") || content.EndsWith("\r"))
        {
            return content[..^1];
        }
        return content;
    }

    private (Encoding? encoding, int bomLength) DetectEncoding(byte[] bytes)
    {
        // BOMチェック
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return (new UTF8Encoding(true), 3);
        }

        // UTF-8（BOMなし）またはASCIIとして扱う
        bool isValidUtf8 = IsValidUtf8(bytes);
        bool isValidShiftJis = IsValidShiftJis(bytes);

        if (isValidUtf8 && !isValidShiftJis)
        {
            return (new UTF8Encoding(false), 0);
        }

        if (!isValidUtf8 && isValidShiftJis)
        {
            return (Encoding.GetEncoding("Shift_JIS"), 0);
        }

        // 両方とも有効な場合、ASCII文字のみならUTF-8として扱う
        if (isValidUtf8 && isValidShiftJis)
        {
            if (IsAsciiOnly(bytes))
            {
                return (new UTF8Encoding(false), 0);
            }
            // ASCII以外で両方とも有効な場合、区別が困難なのでエラー
            return (null, 0);
        }

        return (null, 0);
    }

    private bool IsAsciiOnly(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (b > 127)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            var encoding = new UTF8Encoding(false, true);
            var decoded = encoding.GetString(bytes);
            var reencoded = encoding.GetBytes(decoded);
            
            // 再エンコード結果が元と一致するか確認
            if (reencoded.Length != bytes.Length)
            {
                return false;
            }
            
            for (int i = 0; i < bytes.Length; i++)
            {
                if (reencoded[i] != bytes[i])
                {
                    return false;
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidShiftJis(byte[] bytes)
    {
        try
        {
            var encoding = Encoding.GetEncoding("Shift_JIS");
            var decoded = encoding.GetString(bytes);
            var reencoded = encoding.GetBytes(decoded);
            
            // 再エンコード結果が元と一致するか確認
            if (reencoded.Length != bytes.Length)
            {
                return false;
            }
            
            for (int i = 0; i < bytes.Length; i++)
            {
                if (reencoded[i] != bytes[i])
                {
                    return false;
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
