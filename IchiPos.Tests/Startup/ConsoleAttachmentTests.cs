using IchiPos.Startup;
using Moq;
using Xunit;

namespace IchiPos.Tests.Startup;

public class ConsoleAttachmentTests
{
    [Fact]
    public void 正常系_親コンソールへのアタッチに成功した場合は新規コンソールを割り当てない()
    {
        // Arrange
        var mock = new Mock<IConsoleAttacher>();
        mock.Setup(x => x.AttachToParent()).Returns(true);

        // Act
        ConsoleAttachment.Ensure(mock.Object);

        // Assert
        mock.Verify(x => x.AllocateNew(), Times.Never);
    }

    [Fact]
    public void 正常系_親コンソールへのアタッチに失敗した場合は新規コンソールを割り当てる()
    {
        // Arrange
        // GUIサブシステムのexeを、親コンソールを持たない状態(エクスプローラーからの起動など)で
        // CLIモード実行した場合に相当する。
        var mock = new Mock<IConsoleAttacher>();
        mock.Setup(x => x.AttachToParent()).Returns(false);

        // Act
        ConsoleAttachment.Ensure(mock.Object);

        // Assert
        mock.Verify(x => x.AllocateNew(), Times.Once);
    }
}
