namespace IchiPos.Gui;

/// <summary>
/// 前回投稿内容のハッシュ値を %LOCALAPPDATA%\IchiPos\last_post_hash.txt へ保存するILastPostStore実装
/// (04書 G-015第4節)。設定ファイル(config/config.yaml)はユーザーが編集する設定であるため、
/// アプリケーションが書き換える状態の保存先には用いない。
/// 読み書きの失敗は投稿処理を妨げないよう、例外を投げずに「記録なし」「保存しない」として扱う。
/// </summary>
public class FileLastPostStore : ILastPostStore
{
    private const string FileName = "last_post_hash.txt";

    private readonly string _directory;

    public FileLastPostStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IchiPos"))
    {
    }

    public FileLastPostStore(string directory)
    {
        _directory = directory;
    }

    private string FilePath => Path.Combine(_directory, FileName);

    public string? LoadHash()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;

            var hash = File.ReadAllText(FilePath).Trim();
            return string.IsNullOrEmpty(hash) ? null : hash;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveHash(string hash)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath, hash);
        }
        catch (Exception)
        {
            // 記録できなくても投稿自体は成功しているため、握りつぶす(G-015第4節第5項)。
        }
    }
}
