namespace IchiPos.Gui;

/// <summary>
/// WPF標準のFluentテーマ(<see cref="System.Windows.ThemeMode"/>)を用いた<see cref="IThemeApplier"/>実装
/// (04書 G-019第4節)。System.Windows.Application.Current は通常GUI起動後に生成済みだが、
/// (コードレビューで指摘の通り)テスト等でApplicationが生成されない文脈から呼ばれる可能性があるため、
/// その場合は何もしない(未生成のApplicationへ副作用を起こしようがないため、これはエラーではない)。
/// </summary>
public class WpfThemeApplier : IThemeApplier
{
    public void Apply(string theme)
    {
        var application = System.Windows.Application.Current;
        if (application is null) return;

        application.ThemeMode = ThemeModeResolver.Resolve(theme);
    }
}
