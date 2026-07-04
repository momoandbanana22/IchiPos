using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace IchiPos.Images;

public interface IImageValidator
{
    ImageValidationResult Validate(string folderPath, List<string> fileNames);

    /// <summary>GUI向け: フォルダ結合を行わず、渡されたフルパスをそのまま検証する。</summary>
    ImageValidationResult ValidateFiles(List<string> filePaths);
}

public class ImageValidator : IImageValidator
{
    public ImageValidationResult Validate(string folderPath, List<string> fileNames)
    {
        var fullPaths = fileNames.Select(fileName => Path.Combine(folderPath, fileName)).ToList();
        return ValidateFiles(fullPaths);
    }

    public ImageValidationResult ValidateFiles(List<string> filePaths)
    {
        var validImagePaths = new List<string>();

        foreach (var fullPath in filePaths)
        {
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
