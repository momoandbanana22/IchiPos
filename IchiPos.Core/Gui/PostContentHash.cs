using System.Security.Cryptography;
using System.Text;

namespace IchiPos.Gui;

/// <summary>
/// 前回投稿内容の一致判定に使うハッシュ値の算出(04書 G-015第2節・第4節)。
/// 投稿本文をディスクへ残さないため、比較は本文そのものではなくこのハッシュ値で行う。
/// </summary>
public static class PostContentHash
{
    /// <summary>比較用テキスト(日付置換済み・末尾空白除去済み)のSHA-256ハッシュ値を16進小文字で返す。</summary>
    public static string Compute(string comparableContent)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(comparableContent));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
