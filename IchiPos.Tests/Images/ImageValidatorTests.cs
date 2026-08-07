using IchiPos.Images;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace IchiPos.Tests.Images;

public class ImageValidatorTests
{
    [Fact]
    public void 正常系_有効な画像ファイル()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        // 有効なPNGファイルを作成
        var pngPath = Path.Combine(testDir, "test.png");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(pngPath, ImageFormat.Png);
        }
        
        var validator = new ImageValidator();

        // Act
        var result = validator.Validate(testDir, new List<string> { "test.png" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.ValidImagePaths);
        Assert.Contains(pngPath, result.ValidImagePaths);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_相対フォルダパスでも絶対パスを返す_issue98()
    {
        // #98: --image-path が相対のとき、クリップボードに載るパスが相対のままだと、
        // 外部（X/ブラウザ）がファイルを解決できず貼り付けできない（実機で再現・確定）。
        // Validate はフォルダが相対でも絶対パス（完全修飾）を返さなければならない。
        // Arrange: カレントディレクトリ配下に相対フォルダを作り、実PNGを置く（CWDは変更しない）。
        var relativeDir = "issue98_rel_" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(relativeDir);
        try
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                bitmap.Save(Path.Combine(relativeDir, "test.png"), ImageFormat.Png);
            }

            var validator = new ImageValidator();

            // Act: 相対フォルダパスで検証する
            var result = validator.Validate(relativeDir, new List<string> { "test.png" });

            // Assert: 返るパスはすべて絶対（完全修飾）でなければならない
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.ValidImagePaths);
            Assert.All(result.ValidImagePaths, p => Assert.True(
                Path.IsPathFullyQualified(p),
                $"絶対パスでない（相対だと X に貼り付けできない）: {p}"));
        }
        finally
        {
            Directory.Delete(relativeDir, recursive: true);
        }
    }

    [Fact]
    public void 正常系_複数の有効な画像ファイル()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        var pngPath = Path.Combine(testDir, "test.png");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(pngPath, ImageFormat.Png);
        }
        
        var jpgPath = Path.Combine(testDir, "test.jpg");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(jpgPath, ImageFormat.Jpeg);
        }
        
        var validator = new ImageValidator();

        // Act
        var result = validator.Validate(testDir, new List<string> { "test.png", "test.jpg" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidImagePaths.Count);
        Assert.Contains(pngPath, result.ValidImagePaths);
        Assert.Contains(jpgPath, result.ValidImagePaths);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_画像として読み込めないファイル()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        // PNG拡張子だが画像ではないファイル
        var fakePngPath = Path.Combine(testDir, "fake.png");
        File.WriteAllText(fakePngPath, "not an image");
        
        var validator = new ImageValidator();

        // Act
        var result = validator.Validate(testDir, new List<string> { "fake.png" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_空のファイルリスト()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        var validator = new ImageValidator();

        // Act
        var result = validator.Validate(testDir, new List<string>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.ValidImagePaths);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_一部のファイルが読み込めない()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        var pngPath = Path.Combine(testDir, "test.png");
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        File.WriteAllBytes(pngPath, pngBytes);
        
        var fakePngPath = Path.Combine(testDir, "fake.png");
        File.WriteAllText(fakePngPath, "not an image");
        
        var validator = new ImageValidator();

        // Act
        var result = validator.Validate(testDir, new List<string> { "test.png", "fake.png" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    // ──────────────────────────────────────────────────────────────────
    // ValidateFiles(GUI: フルパスのリストを直接検証。フォルダ結合を行わない)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFiles_正常系_複数の有効な画像ファイル()
    {
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        var pngPath = Path.Combine(testDir, "test.png");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(pngPath, ImageFormat.Png);
        }

        var jpgPath = Path.Combine(testDir, "test.jpg");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(jpgPath, ImageFormat.Jpeg);
        }

        var validator = new ImageValidator();

        var result = validator.ValidateFiles(new List<string> { pngPath, jpgPath });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidImagePaths.Count);
        Assert.Contains(pngPath, result.ValidImagePaths);
        Assert.Contains(jpgPath, result.ValidImagePaths);

        Directory.Delete(testDir, true);
    }

    [Fact]
    public void ValidateFiles_正常系_異なるフォルダのファイルを混在できる()
    {
        var testDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var testDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);

        var pngPath = Path.Combine(testDir1, "test.png");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(pngPath, ImageFormat.Png);
        }

        var jpgPath = Path.Combine(testDir2, "test.jpg");
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.Save(jpgPath, ImageFormat.Jpeg);
        }

        var validator = new ImageValidator();

        var result = validator.ValidateFiles(new List<string> { pngPath, jpgPath });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidImagePaths.Count);

        Directory.Delete(testDir1, true);
        Directory.Delete(testDir2, true);
    }

    [Fact]
    public void ValidateFiles_正常系_空のファイルリスト()
    {
        var validator = new ImageValidator();

        var result = validator.ValidateFiles(new List<string>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.ValidImagePaths);
    }

    [Fact]
    public void ValidateFiles_異常系_画像として読み込めないファイル()
    {
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        var fakePngPath = Path.Combine(testDir, "fake.png");
        File.WriteAllText(fakePngPath, "not an image");

        var validator = new ImageValidator();

        var result = validator.ValidateFiles(new List<string> { fakePngPath });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        Directory.Delete(testDir, true);
    }

    [Fact]
    public void ValidateFiles_異常系_存在しないファイル()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");

        var validator = new ImageValidator();

        var result = validator.ValidateFiles(new List<string> { missingPath });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
