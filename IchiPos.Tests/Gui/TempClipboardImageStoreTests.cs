using System.Windows.Media.Imaging;
using IchiPos.Gui;
using Xunit;

namespace IchiPos.Tests.Gui;

public class TempClipboardImageStoreTests
{
    private static BitmapSource DummyImage() =>
        new WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

    [Fact]
    public void 正常系_SaveToTempFileは読み込み可能なPNGファイルのパスを返す()
    {
        var store = new TempClipboardImageStore();

        var filePath = store.SaveToTempFile(DummyImage());

        Assert.True(File.Exists(filePath));
        Assert.Equal(".png", Path.GetExtension(filePath));

        store.Delete(filePath);
    }

    [Fact]
    public void 正常系_2回保存すると別々のファイルになる()
    {
        var store = new TempClipboardImageStore();

        var filePath1 = store.SaveToTempFile(DummyImage());
        var filePath2 = store.SaveToTempFile(DummyImage());

        Assert.NotEqual(filePath1, filePath2);
        Assert.True(File.Exists(filePath1));
        Assert.True(File.Exists(filePath2));

        store.Delete(filePath1);
        store.Delete(filePath2);
    }

    [Fact]
    public void 正常系_複数回保存してもファイル名が重複しない()
    {
        // X下書き画面へは複数の一時ファイルをファイルドロップリストとして同時に渡す（F-008第5項）。
        // このときファイル名が重複していると、貼り付け先が同一ファイルとみなして畳んでしまうため、
        // 親フォルダだけでなくファイル名自体が一意である必要がある。
        var store = new TempClipboardImageStore();

        var filePaths = Enumerable.Range(0, 4).Select(_ => store.SaveToTempFile(DummyImage())).ToList();

        var fileNames = filePaths.Select(Path.GetFileName).ToList();
        Assert.Equal(fileNames.Count, fileNames.Distinct().Count());

        foreach (var filePath in filePaths) store.Delete(filePath);
    }

    [Fact]
    public void 正常系_Deleteでファイルとその親フォルダを削除する()
    {
        var store = new TempClipboardImageStore();
        var filePath = store.SaveToTempFile(DummyImage());
        var folder = Path.GetDirectoryName(filePath)!;

        store.Delete(filePath);

        Assert.False(File.Exists(filePath));
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void 正常系_他のファイルのDeleteに影響しない()
    {
        var store = new TempClipboardImageStore();
        var filePath1 = store.SaveToTempFile(DummyImage());
        var filePath2 = store.SaveToTempFile(DummyImage());

        store.Delete(filePath1);

        Assert.False(File.Exists(filePath1));
        Assert.True(File.Exists(filePath2));

        store.Delete(filePath2);
    }
}
