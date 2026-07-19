using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>04書 G-015第4節: 前回投稿内容(ハッシュ値)の永続化。</summary>
public class FileLastPostStoreTests : IDisposable
{
    private readonly string _directory;

    public FileLastPostStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "IchiPosTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private FileLastPostStore CreateStore() => new FileLastPostStore(_directory);

    [Fact]
    public void 正常系_記録がない場合はnullを返す()
    {
        Assert.Null(CreateStore().LoadHash());
    }

    [Fact]
    public void 正常系_保存したハッシュ値を読み出せる()
    {
        var store = CreateStore();
        store.SaveHash("abc123");

        Assert.Equal("abc123", store.LoadHash());
    }

    [Fact]
    public void 正常系_別インスタンスからも読み出せる()
    {
        // アプリケーション再起動をまたいで保持することの確認(G-015第4節第4項)
        CreateStore().SaveHash("abc123");

        Assert.Equal("abc123", CreateStore().LoadHash());
    }

    [Fact]
    public void 正常系_保存のたびに上書きし履歴は保持しない()
    {
        var store = CreateStore();
        store.SaveHash("first");
        store.SaveHash("second");

        Assert.Equal("second", store.LoadHash());
    }

    [Fact]
    public void 正常系_投稿本文そのものはファイルに残さない()
    {
        var store = CreateStore();
        var hash = PostContentHash.Compute("秘密の投稿本文");
        store.SaveHash(hash);

        var saved = File.ReadAllText(Path.Combine(_directory, "last_post_hash.txt"));
        Assert.DoesNotContain("秘密の投稿本文", saved);
        Assert.Contains(hash, saved);
    }

    [Fact]
    public void 異常系_保存先を作成できない場合も例外を投げない()
    {
        // 既に同名のファイルが存在するパスはフォルダとして作成できない
        var filePath = Path.Combine(Path.GetTempPath(), "IchiPosTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "");
        try
        {
            var store = new FileLastPostStore(filePath);

            store.SaveHash("abc123");
            Assert.Null(store.LoadHash());
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void 正常系_記録ファイルが空の場合はnullを返す()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "last_post_hash.txt"), "  \r\n");

        Assert.Null(CreateStore().LoadHash());
    }
}
