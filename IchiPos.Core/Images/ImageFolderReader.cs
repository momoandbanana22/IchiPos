namespace IchiPos.Images;

public interface IImageFolderReader
{
    ImageFolderReadResult Read(string? folderPath);
}

public class ImageFolderReader : IImageFolderReader
{
    private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".gif" };

    public ImageFolderReadResult Read(string? folderPath)
    {
        // 未指定の場合は空のリストを返す
        if (string.IsNullOrEmpty(folderPath))
        {
            return ImageFolderReadResult.Success(new List<string>());
        }

        // フォルダが存在しない場合はエラー
        if (!Directory.Exists(folderPath))
        {
            return ImageFolderReadResult.Failure("フォルダが存在しません");
        }

        try
        {
            var imageFiles = Directory.GetFiles(folderPath)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .OrderBy(name => name!)
                .Select(name => name!)
                .ToList();

            return ImageFolderReadResult.Success(imageFiles);
        }
        catch (Exception ex)
        {
            return ImageFolderReadResult.Failure($"フォルダの読み込みに失敗しました: {ex.Message}");
        }
    }
}
