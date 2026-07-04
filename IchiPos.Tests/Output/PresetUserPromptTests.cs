using IchiPos.Output;
using Xunit;

namespace IchiPos.Tests.Output;

public class PresetUserPromptTests
{
    [Fact]
    public void 正常系_事前設定がtrueの場合はyを返す()
    {
        // Arrange
        var prompt = new PresetUserPrompt(() => true);

        // Act
        var answer = prompt.Ask("画像を削除してよいですか？（1枚）(y/n): ");

        // Assert
        Assert.Equal("y", answer);
    }

    [Fact]
    public void 正常系_事前設定がfalseの場合はnを返す()
    {
        // Arrange
        var prompt = new PresetUserPrompt(() => false);

        // Act
        var answer = prompt.Ask("画像を削除してよいですか？（1枚）(y/n): ");

        // Assert
        Assert.Equal("n", answer);
    }

    [Fact]
    public void 正常系_呼び出しのたびに現在の設定値を参照する()
    {
        // Arrange
        // 04書 G-004 第5節: 投稿実行時点でのチェックボックスの状態を参照する。
        var isChecked = false;
        var prompt = new PresetUserPrompt(() => isChecked);

        // Act
        var before = prompt.Ask("q");
        isChecked = true;
        var after = prompt.Ask("q");

        // Assert
        Assert.Equal("n", before);
        Assert.Equal("y", after);
    }
}
