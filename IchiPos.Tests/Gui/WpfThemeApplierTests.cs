using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>
/// System.Windows.Applicationが生成されていない文脈(xunitのテストプロセス全体がこれに該当。
/// GuiEntryPoint.Runのみがnew Application()を行うが、GuiEntryPointはユニットテスト対象外)での
/// 呼び出し安全性を検証する(コードレビューで指摘されたApplication.Current無検証参照への対処)。
/// </summary>
public class WpfThemeApplierTests
{
    [Fact]
    public void 異常系_ApplicationCurrentが未生成でも例外にならない()
    {
        Assert.Null(System.Windows.Application.Current);

        var applier = new WpfThemeApplier();
        var exception = Record.Exception(() => applier.Apply("dark"));

        Assert.Null(exception);
    }
}
