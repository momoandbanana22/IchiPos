using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IchiPos.Config;

public class AppConfig
{
    public MisskeyConfig Misskey { get; set; } = new();
    public XConfig X { get; set; } = new();
    public Mixi2Config Mixi2 { get; set; } = new();
    public LimitsConfig Limits { get; set; } = new();
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
    public bool Enabled { get; set; } = false;
}

public class Mixi2Config
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class LimitsConfig
{
    public int MisskeyMaxLength { get; set; } = 5000;
    public int XMaxLength { get; set; } = 280;
    public int Mixi2MaxLength { get; set; } = 280;
}
