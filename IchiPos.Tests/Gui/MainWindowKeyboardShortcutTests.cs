using System.Windows.Input;
using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>投稿内容欄でのCtrl+Enter投稿ショートカットの判定ロジック(04書 G-014)。</summary>
public class MainWindowKeyboardShortcutTests
{
    [Fact]
    public void 正常系_CtrlEnterは投稿ショートカットと判定する()
    {
        Assert.True(MainWindow.IsPostShortcut(Key.Enter, ModifierKeys.Control));
    }

    [Fact]
    public void 異常系_修飾キーなしのEnterは投稿ショートカットと判定しない()
    {
        Assert.False(MainWindow.IsPostShortcut(Key.Enter, ModifierKeys.None));
    }

    [Fact]
    public void 異常系_CtrlShiftEnterは投稿ショートカットと判定しない()
    {
        Assert.False(MainWindow.IsPostShortcut(Key.Enter, ModifierKeys.Control | ModifierKeys.Shift));
    }

    [Fact]
    public void 異常系_Ctrl押下でもEnter以外は投稿ショートカットと判定しない()
    {
        Assert.False(MainWindow.IsPostShortcut(Key.V, ModifierKeys.Control));
    }
}
