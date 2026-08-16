namespace IchiPos.Gui;

/// <summary>設定のtheme値(04書 G-019)に応じて、実行中のアプリの外観を切り替える。</summary>
public interface IThemeApplier
{
    void Apply(string theme);
}
