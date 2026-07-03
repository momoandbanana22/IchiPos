using IchiPos.Content;
using Moq;
using Xunit;

namespace IchiPos.Tests.Content;

public class ContentResolverTests
{
    private static Mock<IDatePlaceholderReplacer> CreatePassThroughDateReplacerMock()
    {
        var mock = new Mock<IDatePlaceholderReplacer>();
        mock.Setup(x => x.Replace(It.IsAny<string>()))
            .Returns((string s) => s);
        return mock;
    }

    [Fact]
    public async Task 正常系_文字列を指定()
    {
        // Arrange
        var content = "hello";
        var mockTextFileReader = new Mock<ITextFileReader>();
        var mockDateReplacer = CreatePassThroughDateReplacerMock();
        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);
        mockTextFileReader.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_文字列を指定_末尾に改行があってもそのまま保持される()
    {
        // Arrange
        var content = "hello\n";
        var mockTextFileReader = new Mock<ITextFileReader>();
        var mockDateReplacer = CreatePassThroughDateReplacerMock();
        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Content);
        mockTextFileReader.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_存在するtxtファイルを指定_実際のTextFileReaderで末尾改行が除去される()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "ファイル内容\n", new System.Text.UTF8Encoding(false));

        var resolver = new ContentResolver(new TextFileReader(), new DatePlaceholderReplacer(TimeProvider.System));

        // Act
        var result = await resolver.ResolveAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ファイル内容", result.Content);

        // Cleanup
        Directory.Delete(testDir, true);
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
        var mockDateReplacer = CreatePassThroughDateReplacerMock();

        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

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
        var mockDateReplacer = CreatePassThroughDateReplacerMock();

        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

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
        var mockDateReplacer = CreatePassThroughDateReplacerMock();

        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

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
        var mockDateReplacer = CreatePassThroughDateReplacerMock();
        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        mockTextFileReader.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_文字列指定で日付プレースホルダが置換される()
    {
        // Arrange
        var content = "今日は{date}です";
        var mockTextFileReader = new Mock<ITextFileReader>();
        var mockDateReplacer = new Mock<IDatePlaceholderReplacer>();
        mockDateReplacer.Setup(x => x.Replace(content)).Returns("今日は2026/07/03です");

        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

        // Act
        var result = await resolver.ResolveAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("今日は2026/07/03です", result.Content);
        mockDateReplacer.Verify(x => x.Replace(content), Times.Once);
    }

    [Fact]
    public async Task 正常系_txtファイル指定で日付プレースホルダが置換される()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "test.txt");
        var fileContent = "今日は{date}です";
        await File.WriteAllTextAsync(filePath, fileContent);

        var mockTextFileReader = new Mock<ITextFileReader>();
        mockTextFileReader.Setup(x => x.ReadAsync(filePath))
            .ReturnsAsync(TextFileReadResult.Success(fileContent));
        var mockDateReplacer = new Mock<IDatePlaceholderReplacer>();
        mockDateReplacer.Setup(x => x.Replace(fileContent)).Returns("今日は2026/07/03です");

        var resolver = new ContentResolver(mockTextFileReader.Object, mockDateReplacer.Object);

        // Act
        var result = await resolver.ResolveAsync(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("今日は2026/07/03です", result.Content);
        mockDateReplacer.Verify(x => x.Replace(fileContent), Times.Once);

        // Cleanup
        Directory.Delete(testDir, true);
    }
}
