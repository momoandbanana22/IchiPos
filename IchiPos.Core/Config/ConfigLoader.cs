using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IchiPos.Config;

public interface IConfigLoader
{
    ConfigLoadResult Load(string baseDirectory);
}

public class ConfigLoader : IConfigLoader
{
    public ConfigLoadResult Load(string baseDirectory)
    {
        var configDir = Path.Combine(baseDirectory, "config");
        var configPath = Path.Combine(configDir, "config.yaml");

        if (!File.Exists(configPath))
        {
            return ConfigLoadResult.Failure("設定ファイルが存在しません");
        }

        try
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<AppConfig>(yaml);
            if (config is null)
            {
                return ConfigLoadResult.Failure("設定ファイルが空です");
            }

            if (string.IsNullOrWhiteSpace(config.Misskey.InstanceUrl))
                return ConfigLoadResult.Failure("設定 misskey.instance_url が設定されていません");
            if (string.IsNullOrWhiteSpace(config.Misskey.AccessToken))
                return ConfigLoadResult.Failure("設定 misskey.access_token が設定されていません");
            if (string.IsNullOrWhiteSpace(config.X.PostUrlBase))
                return ConfigLoadResult.Failure("設定 x.post_url_base が設定されていません");

            // 定型文(G-016第2節)。任意設定のため未記載でもエラーとしない。
            // 空文字・空白のみの項目は、投稿しても投稿前チェックでエラーになるだけなので画面に並べない(第4項)。
            config.Templates = (config.Templates ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return ConfigLoadResult.Success(config);
        }
        catch (Exception ex)
        {
            return ConfigLoadResult.Failure($"設定ファイルの読み込みに失敗しました: {ex.Message}");
        }
    }
}
