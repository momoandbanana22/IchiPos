using IchiPos.Config;
using IchiPos.Post;
using Moq;
using Xunit;

namespace IchiPos.Tests.Post;

public class PostDestinationRunnerTests
{
    [Fact]
    public async Task 正常系_MIXI2とXを登録順に実行する()
    {
        // Arrange
        var config = new AppConfig();
        var callOrder = new List<string>();

        var mockMixi2 = new Mock<IMixi2Poster>();
        mockMixi2.Setup(x => x.PostAsync("content", It.IsAny<List<string>>(), config))
            .Callback(() => callOrder.Add("mixi2"))
            .ReturnsAsync(Mixi2PostResult.Success("post_id"));

        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync("content", config))
            .Callback(() => callOrder.Add("x"))
            .ReturnsAsync(XPostLaunchResult.Success());

        var runner = new PostDestinationRunner(mockMixi2.Object, mockX.Object);

        // Act
        var result = await runner.RunAsync("content", new List<string>(), config);

        // Assert
        Assert.Equal(new[] { "mixi2", "x" }, callOrder);
        Assert.True(result.Mixi2.IsSuccess);
        Assert.True(result.X.IsSuccess);
    }

    [Fact]
    public async Task 正常系_MIXI2が失敗してもXは実行される()
    {
        // Arrange
        var config = new AppConfig();
        var mockMixi2 = new Mock<IMixi2Poster>();
        mockMixi2.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Failure("MIXI2投稿失敗"));

        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), config))
            .ReturnsAsync(XPostLaunchResult.Success());

        var runner = new PostDestinationRunner(mockMixi2.Object, mockX.Object);

        // Act
        var result = await runner.RunAsync("content", new List<string>(), config);

        // Assert
        Assert.False(result.Mixi2.IsSuccess);
        Assert.True(result.X.IsSuccess);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_MIXI2がスキップされてもXは実行される()
    {
        // Arrange
        var config = new AppConfig();
        var mockMixi2 = new Mock<IMixi2Poster>();
        mockMixi2.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Skipped());

        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), config))
            .ReturnsAsync(XPostLaunchResult.Success());

        var runner = new PostDestinationRunner(mockMixi2.Object, mockX.Object);

        // Act
        var result = await runner.RunAsync("content", new List<string>(), config);

        // Assert
        Assert.True(result.Mixi2.IsSkipped);
        mockX.Verify(x => x.LaunchAsync(It.IsAny<string>(), config), Times.Once);
    }

    [Fact]
    public async Task 正常系_Xが失敗してもMIXI2の実行結果は保持される()
    {
        // Arrange
        var config = new AppConfig();
        var mockMixi2 = new Mock<IMixi2Poster>();
        mockMixi2.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<string>>(), config))
            .ReturnsAsync(Mixi2PostResult.Success("post_id"));

        var mockX = new Mock<IXPostLauncher>();
        mockX.Setup(x => x.LaunchAsync(It.IsAny<string>(), config))
            .ReturnsAsync(XPostLaunchResult.Failure("X起動失敗"));

        var runner = new PostDestinationRunner(mockMixi2.Object, mockX.Object);

        // Act
        var result = await runner.RunAsync("content", new List<string>(), config);

        // Assert
        Assert.True(result.Mixi2.IsSuccess);
        Assert.False(result.X.IsSuccess);
    }
}
