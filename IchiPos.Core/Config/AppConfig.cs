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
