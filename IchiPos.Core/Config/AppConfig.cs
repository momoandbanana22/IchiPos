using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IchiPos.Config;

public class AppConfig
{
    public MisskeyConfig Misskey { get; set; } = new();
    public XConfig X { get; set; } = new();
    public LimitsConfig Limits { get; set; } = new();

    /// <summary>
    /// 定型文(04書 G-016 第2節)。GUIの「定型文」タブに、登録順のままワンクリック投稿ボタンとして並ぶ。
    /// 任意設定であり、未記載の場合は0件として扱う(設定読み込みエラーとしない)。
    /// </summary>
    public List<string> Templates { get; set; } = new();

    /// <summary>
    /// ダークモード切替(04書 G-019、issue #109)。任意設定であり、未記載の場合は既定値"system"で有効。
    /// 許容値は system(OSのテーマ設定に追従) / light / dark。
    /// </summary>
    public string Theme { get; set; } = "system";
}

public class MisskeyConfig
{
    public string InstanceUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string Visibility { get; set; } = "public";
}

public class XConfig
{
    public string PostUrlBase { get; set; } = string.Empty;
}

public class LimitsConfig
{
    public int MisskeyMaxLength { get; set; } = 5000;
    public int XMaxLength { get; set; } = 280;
}
