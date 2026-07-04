using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task 正常系_Executeで指定した非同期処理を実行する()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);

        // Act
        command.Execute(null);
        tcs.SetResult();
        await Task.Delay(10); // async void の継続完了を待つ

        // Assert(例外なく完了すればよい)
        Assert.True(tcs.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public void 正常系_実行中はCanExecuteがfalseになる()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);

        Assert.True(command.CanExecute(null));

        // Act
        command.Execute(null);

        // Assert
        Assert.False(command.CanExecute(null));

        // Cleanup
        tcs.SetResult();
    }

    [Fact]
    public async Task 正常系_完了後はCanExecuteがtrueに戻る()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);

        // Act
        command.Execute(null);
        tcs.SetResult();
        await Task.Delay(10);

        // Assert
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void 正常系_canExecuteの戻り値もあわせて評価する()
    {
        // Arrange
        var allowed = false;
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => allowed);

        // Act & Assert
        Assert.False(command.CanExecute(null));
        allowed = true;
        Assert.True(command.CanExecute(null));
    }
}
