using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IchiPos.Gui;

/// <summary>サムネイル一覧(04書 G-013)用: 画像ファイルパスをDecodePixelWidthで軽量デコードしたBitmapImageに変換する。</summary>
public class FilePathToThumbnailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || !File.Exists(filePath)) return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 96;
        bitmap.UriSource = new Uri(filePath);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
