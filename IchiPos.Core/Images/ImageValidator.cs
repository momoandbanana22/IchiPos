using System.Drawing;
using System.Drawing.Imaging;

namespace IchiPos.Images;

public interface IImageValidator
{
    ImageValidationResult Validate(string folderPath, List<string> fileNames);
}

public class ImageValidator : IImageValidator
{
    public ImageValidationResult Validate(string folderPath, List<string> fileNames)
    {
        var validImagePaths = new List<string>();

        foreach (var fileName in fileNames)
        {
            var fullPath = Path.Combine(folderPath, fileName);

            try
            {
                // 画像として読み込み可能か確認
                using (var image = Image.FromFile(fullPath))
                {
                    // 読み込み成功なら有効
                    validImagePaths.Add(fullPath);
                }
            }
            catch
            {
                // いずれかのファイルが読み込めない場合は全体エラー
                return ImageValidationResult.Failure("画像として読み込めないファイルが含まれています");
            }
        }

        return ImageValidationResult.Success(validImagePaths);
    }
}
