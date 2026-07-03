using System.Text;
using IchiPos.Content;
using Xunit;

namespace IchiPos.Tests.Content;

public class TextFileReaderTests
{
    [Fact]
    public async Task 正常系_UTF8_BOMあり()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var content = "テスト内容";
        await File.WriteAllTextAsync(filePath, content, new System.Text.UTF8Encoding(true));
        
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_UTF8_BOMなし()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var content = "テスト内容";
        await File.WriteAllTextAsync(filePath, content, new System.Text.UTF8Encoding(false));
        
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_Shift_JIS()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var content = "テスト内容";
        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.GetEncoding("Shift_JIS"));
        
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_ASCII()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var content = "Hello World";
        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.ASCII);
        
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_末尾LFが1つ除去される()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllBytesAsync(filePath, new UTF8Encoding(false).GetBytes("テスト内容\n"));

        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト内容", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_末尾CRLFが1つ除去される()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllBytesAsync(filePath, new UTF8Encoding(false).GetBytes("テスト内容\r\n"));

        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト内容", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_末尾に改行が複数ある場合は1つだけ除去される()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllBytesAsync(filePath, new UTF8Encoding(false).GetBytes("テスト内容\n\n"));

        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト内容\n", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_末尾に改行がない場合は変化しない()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllBytesAsync(filePath, new UTF8Encoding(false).GetBytes("テスト内容"));

        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト内容", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 正常系_末尾のスペースは改行ではないため除去されない()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllBytesAsync(filePath, new UTF8Encoding(false).GetBytes("テスト内容  "));

        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト内容  ", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 異常系_ファイルが存在しない()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.txt");
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task 異常系_対応外のエンコード()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var content = "テスト内容";
        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF32);
        
        var reader = new TextFileReader();

        // Act
        var result = await reader.ReadAsync(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }
}
