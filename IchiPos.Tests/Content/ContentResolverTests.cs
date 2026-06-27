using IchiPos.Content;
using Moq;
using Xunit;

namespace IchiPos.Tests.Content;

public class ContentResolverTests
{
    [Fact]
    public async Task 正常系_文字列を指定()
    {
        // Arrange
        var content = "hello";
        var mockTextFileReader = new Mock<ITextFileReader>();
        var resolver = new ContentResolver(mockTextFileReader.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);
        mockTextFileReader.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_存在するtxtファイルを指定()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var fileContent = "ファイル内容";
        await File.WriteAllTextAsync(filePath, fileContent);
        
        var mockTextFileReader = new Mock<ITextFileReader>();
        mockTextFileReader.Setup(x => x.ReadAsync(filePath))
            .ReturnsAsync(TextFileReadResult.Success(fileContent));
        
        var resolver = new ContentResolver(mockTextFileReader.Object);

        // Act
        var result = await resolver.ResolveAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileContent, result.Content);
        mockTextFileReader.Verify(x => x.ReadAsync(filePath), Times.Once);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 異常系_存在しないtxtファイルを指定()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.txt");
        var mockTextFileReader = new Mock<ITextFileReader>();
        mockTextFileReader.Setup(x => x.ReadAsync(filePath))
            .ReturnsAsync(TextFileReadResult.Failure("ファイルが存在しません"));
        
        var resolver = new ContentResolver(mockTextFileReader.Object);

        // Act
        var result = await resolver.ResolveAsync(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task 異常系_txt拡張子だがファイル読み込み失敗()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "test");
        
        var mockTextFileReader = new Mock<ITextFileReader>();
        mockTextFileReader.Setup(x => x.ReadAsync(filePath))
            .ReturnsAsync(TextFileReadResult.Failure("読み込み失敗"));
        
        var resolver = new ContentResolver(mockTextFileReader.Object);

        // Act
        var result = await resolver.ResolveAsync(filePath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        // Cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public async Task 異常系_txt拡張子だがファイルが存在しない()
    {
        // Arrange
        var content = "missing.txt";
        var mockTextFileReader = new Mock<ITextFileReader>();
        var resolver = new ContentResolver(mockTextFileReader.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        mockTextFileReader.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }
}
