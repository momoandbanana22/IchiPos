using System.Diagnostics;
using IchiPos.Post;
using Xunit;

namespace IchiPos.Tests.Post;

public class SystemProcessStarterTests
{
    [Fact]
    public void 正常系_ProcessStartがnullを返してもtrueを返す()
    {
        // Arrange
        // Windows では UseShellExecute=true で URL を渡すと
        // シェルがブラウザへ委譲し Process.Start が null を返す（正常）
        var starter = new SystemProcessStarter(psi => null);

        // Act
        var result = starter.Start("https://twitter.com/intent/tweet?text=hello");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void 正常系_ProcessStartがProcessオブジェクトを返す場合もtrueを返す()
    {
        // Arrange
        var starter = new SystemProcessStarter(psi => new Process());

        // Act
        var result = starter.Start("https://example.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void 異常系_ProcessStartが例外をスローした場合は例外を伝播する()
    {
        // Arrange
        var starter = new SystemProcessStarter(
            psi => throw new InvalidOperationException("ブラウザが見つかりません"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => starter.Start("https://example.com"));
    }
}
