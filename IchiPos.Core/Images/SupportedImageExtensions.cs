using System.Linq;

namespace IchiPos.Images;

/// <summary>投稿添付画像として対応する拡張子の一覧(フォルダ読み込み・複数ファイル貼り付けの両方で共用)。</summary>
public static class SupportedImageExtensions
{
    public static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".gif" };

    public static bool IsSupported(string filePath) =>
        Extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());
}
