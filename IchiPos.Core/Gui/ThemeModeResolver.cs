using System.Windows;

namespace IchiPos.Gui;

/// <summary>
/// 設定のtheme値(04書 G-019第2節)をWPFのThemeModeへ変換する。ConfigLoaderで許容値
/// (system/light/dark)を検証済みの値のみを受け取る前提とする。
/// </summary>
public static class ThemeModeResolver
{
    public static ThemeMode Resolve(string theme) => theme switch
    {
        "system" => ThemeMode.System,
        "light" => ThemeMode.Light,
        "dark" => ThemeMode.Dark,
        _ => throw new ArgumentException($"未対応のtheme値です: '{theme}'", nameof(theme)),
    };
}
