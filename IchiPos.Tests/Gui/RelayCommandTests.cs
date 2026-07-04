using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

public class RelayCommandTests
{
    [Fact]
    public void 正常系_Executeで指定したActionを実行する()
    {
        // Arrange
        var called = false;
        var command = new RelayCommand(() => called = true);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void 正常系_canExecute未指定時はCanExecuteが常にtrue()
    {
        // Arrange
        var command = new RelayCommand(() => { });

        // Act & Assert
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void 正常系_canExecuteの戻り値をそのまま反映する()
    {
        // Arrange
        var allowed = false;
        var command = new RelayCommand(() => { }, () => allowed);

        // Act & Assert
        Assert.False(command.CanExecute(null));
        allowed = true;
        Assert.True(command.CanExecute(null));
    }
}
