using IchiPos.Images;
using IchiPos.Output;
using Moq;
using Xunit;

namespace IchiPos.Tests.Images;

public class ImageCleanupServiceTests
{
    [Fact]
    public async Task 正常系_yesと答えたら画像を削除する()
    {
        // Arrange
        var deletedPaths = new List<string>();
        var mockPrompt = new Mock<IUserPrompt>();
        mockPrompt.Setup(x => x.Ask(It.IsAny<string>())).Returns("y");
        var service = new ImageCleanupService(
            mockPrompt.Object,
            Mock.Of<IOutputWriter>(),
            path => deletedPaths.Add(path));
        var imagePaths = new List<string> { "a.png", "b.png" };

        // Act
        await service.RunAsync(imagePaths);

        // Assert
        Assert.Equal(2, deletedPaths.Count);
        Assert.Contains("a.png", deletedPaths);
        Assert.Contains("b.png", deletedPaths);
    }

    [Fact]
    public async Task 正常系_yesの大文字小文字を区別しない()
    {
        // Arrange
        var deletedPaths = new List<string>();
        var mockPrompt = new Mock<IUserPrompt>();
        mockPrompt.Setup(x => x.Ask(It.IsAny<string>())).Returns("Y");
        var service = new ImageCleanupService(
            mockPrompt.Object,
            Mock.Of<IOutputWriter>(),
            path => deletedPaths.Add(path));

        // Act
        await service.RunAsync(new List<string> { "a.png" });

        // Assert
        Assert.Single(deletedPaths);
    }

    [Fact]
    public async Task 正常系_noと答えたら画像を削除しない()
    {
        // Arrange
        var deletedPaths = new List<string>();
        var mockPrompt = new Mock<IUserPrompt>();
        mockPrompt.Setup(x => x.Ask(It.IsAny<string>())).Returns("n");
        var service = new ImageCleanupService(
            mockPrompt.Object,
            Mock.Of<IOutputWriter>(),
            path => deletedPaths.Add(path));

        // Act
        await service.RunAsync(new List<string> { "a.png", "b.png" });

        // Assert
        Assert.Empty(deletedPaths);
    }

    [Fact]
    public async Task 正常系_画像が0枚のとき質問しない()
    {
        // Arrange
        var mockPrompt = new Mock<IUserPrompt>();
        var service = new ImageCleanupService(
            mockPrompt.Object,
            Mock.Of<IOutputWriter>(),
            _ => { });

        // Act
        await service.RunAsync(new List<string>());

        // Assert
        mockPrompt.Verify(x => x.Ask(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task 正常系_削除後に完了メッセージを出力する()
    {
        // Arrange
        var mockPrompt = new Mock<IUserPrompt>();
        mockPrompt.Setup(x => x.Ask(It.IsAny<string>())).Returns("y");
        var mockOutput = new Mock<IOutputWriter>();
        var service = new ImageCleanupService(mockPrompt.Object, mockOutput.Object, _ => { });

        // Act
        await service.RunAsync(new List<string> { "a.png" });

        // Assert
        mockOutput.Verify(
            x => x.WriteInfo(It.Is<string>(s => s.Contains("削除"))),
            Times.Once);
    }

    [Fact]
    public async Task 正常系_スキップ後にスキップメッセージを出力する()
    {
        // Arrange
        var mockPrompt = new Mock<IUserPrompt>();
        mockPrompt.Setup(x => x.Ask(It.IsAny<string>())).Returns("n");
        var mockOutput = new Mock<IOutputWriter>();
        var service = new ImageCleanupService(mockPrompt.Object, mockOutput.Object, _ => { });

        // Act
        await service.RunAsync(new List<string> { "a.png" });

        // Assert
        mockOutput.Verify(
            x => x.WriteInfo(It.Is<string>(s => s.Contains("スキップ"))),
            Times.Once);
    }
}
