using System.Windows;
using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>
/// theme設定値(04書 G-019)をWPFのThemeModeへ変換する純粋関数のテスト。
/// ConfigLoaderが許容値(system/light/dark)を検証済みであることを前提とする(issue #109)。
/// </summary>
public class ThemeModeResolverTests
{
    [Fact]
    public void 正常系_systemはThemeModeSystemに変換される()
    {
        Assert.Equal(ThemeMode.System, ThemeModeResolver.Resolve("system"));
    }

    [Fact]
    public void 正常系_lightはThemeModeLightに変換される()
    {
        Assert.Equal(ThemeMode.Light, ThemeModeResolver.Resolve("light"));
    }

    [Fact]
    public void 正常系_darkはThemeModeDarkに変換される()
    {
        Assert.Equal(ThemeMode.Dark, ThemeModeResolver.Resolve("dark"));
    }

    [Fact]
    public void 異常系_未対応の値は例外を投げる()
    {
        // ConfigLoaderが許容値を検証済みの前提のため通常は起こらないが、theme許容値が
        // ConfigLoader側にのみ追加されResolver側の更新が漏れた場合に、system へ静かにフォール
        // バックせず気付けるようにする(コードレビューで指摘された二重管理リスクへの対処)。
        Assert.Throws<ArgumentException>(() => ThemeModeResolver.Resolve("blue"));
    }
}
