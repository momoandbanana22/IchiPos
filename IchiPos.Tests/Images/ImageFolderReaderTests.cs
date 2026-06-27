using IchiPos.Images;
using Xunit;

namespace IchiPos.Tests.Images;

public class ImageFolderReaderTests
{
    [Fact]
    public void 正常系_画像フォルダ未指定()
    {
        // Arrange
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.ImageFiles);
    }

    [Fact]
    public void 正常系_画像フォルダ指定_対応画像あり()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "image1.png"), "fake png");
        File.WriteAllText(Path.Combine(testDir, "image2.jpg"), "fake jpg");
        File.WriteAllText(Path.Combine(testDir, "image3.jpeg"), "fake jpeg");
        File.WriteAllText(Path.Combine(testDir, "image4.gif"), "fake gif");
        File.WriteAllText(Path.Combine(testDir, "readme.txt"), "text file");
        
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.ImageFiles.Count);
        Assert.Contains("image1.png", result.ImageFiles);
        Assert.Contains("image2.jpg", result.ImageFiles);
        Assert.Contains("image3.jpeg", result.ImageFiles);
        Assert.Contains("image4.gif", result.ImageFiles);
        Assert.DoesNotContain("readme.txt", result.ImageFiles);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_画像フォルダ指定_空フォルダ()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.ImageFiles);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_画像フォルダ指定_対応画像なし()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "readme.txt"), "text file");
        File.WriteAllText(Path.Combine(testDir, "data.json"), "json file");
        
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.ImageFiles);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 異常系_存在しないフォルダ()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 正常系_拡張子が大文字の画像ファイルも取得できる()
    {
        // Arrange
        // ToLower() はカルチャ依存（トルコ語ロケールで .GIF → .gıf になり検出失敗）。
        // ToLowerInvariant() を使うべき。
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "image1.PNG"), "fake png");
        File.WriteAllText(Path.Combine(testDir, "image2.GIF"), "fake gif");

        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ImageFiles.Count);
        Assert.Contains("image1.PNG", result.ImageFiles);
        Assert.Contains("image2.GIF", result.ImageFiles);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void 正常系_ファイル名昇順()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "z.png"), "fake png");
        File.WriteAllText(Path.Combine(testDir, "a.png"), "fake png");
        File.WriteAllText(Path.Combine(testDir, "m.png"), "fake png");
        
        var reader = new ImageFolderReader();

        // Act
        var result = reader.Read(testDir);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ImageFiles.Count);
        Assert.Equal("a.png", result.ImageFiles[0]);
        Assert.Equal("m.png", result.ImageFiles[1]);
        Assert.Equal("z.png", result.ImageFiles[2]);

        // Cleanup
        Directory.Delete(testDir, true);
    }
}
