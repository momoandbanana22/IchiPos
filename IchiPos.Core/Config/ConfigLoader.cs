using System.Text;
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
            // config.yaml はユーザーが編集する外部成果物。既定エンコーディング（実行環境依存）に
            // 頼らず UTF-8 で読むことを明示する（境界を越える値は文脈非依存に。issue #102）。
            var yaml = File.ReadAllText(configPath, Encoding.UTF8);
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

            // visibility は Misskey が受理する値に限る。許容外を送ると 400 になり原因が分かりにくいため、
            // 送信前(設定読み込み時)に弾く(issue #100)。未記載は既定 "public"(AppConfig)で有効。
            var visibilityError = ValidateAllowedValue(
                "misskey.visibility", config.Misskey.Visibility, "public", "home", "followers", "specified");
            if (visibilityError is not null) return visibilityError;

            // ダークモード切替のtheme(G-019第2節、issue #109)。visibilityと同じく、許容外の値は
            // 実行時までエラーが分かりにくいため送信前(設定読み込み時)に弾く。
            var themeError = ValidateAllowedValue("theme", config.Theme, "system", "light", "dark");
            if (themeError is not null) return themeError;

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

    /// <summary>
    /// value が allowed のいずれとも一致しない場合、その旨のConfigLoadResult.Failureを返す。
    /// 一致する場合はnull(呼び出し側はnullなら次の検証へ進む)。
    /// </summary>
    private static ConfigLoadResult? ValidateAllowedValue(string fieldName, string value, params string[] allowed)
    {
        if (allowed.Contains(value)) return null;
        return ConfigLoadResult.Failure(
            $"設定 {fieldName} は {string.Join(" / ", allowed)} のいずれかである必要があります（現在: '{value}'）");
    }
}
