namespace IchiPos.Config;

/// <summary>
/// GUI起動後の設定ファイル再読み込み(04書 G-017)。
/// 起動時(G-009)と同じ設定読み込み処理を、同じ場所に対してその場で再実行するための抽象。
/// </summary>
public interface IConfigReloader
{
    ConfigLoadResult Reload();
}

/// <summary>
/// 起動時と同じ設定読み込み処理(<see cref="IConfigLoader"/>)を、起動時と同じベースディレクトリに対して
/// 再実行する(04書 G-017 第3節)。設定ファイルの編集・保存は行わず、ファイルからの読み込みのみを行う。
/// </summary>
public class ConfigReloader : IConfigReloader
{
    private readonly IConfigLoader _loader;
    private readonly string _baseDirectory;

    public ConfigReloader(IConfigLoader loader, string baseDirectory)
    {
        _loader = loader;
        _baseDirectory = baseDirectory;
    }

    public ConfigLoadResult Reload() => _loader.Load(_baseDirectory);
}
