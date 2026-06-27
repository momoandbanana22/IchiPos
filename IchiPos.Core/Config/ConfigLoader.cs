using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IchiPos.Config;

public class ConfigLoader
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
            return ConfigLoadResult.Success(config!);
        }
        catch (Exception ex)
        {
            return ConfigLoadResult.Failure($"設定ファイルの読み込みに失敗しました: {ex.Message}");
        }
    }
}
