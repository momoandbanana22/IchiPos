using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IchiPos.Gui;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private System.Windows.Point _thumbnailDragStartPoint;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 04書 G-010: クリップボードに画像またはファイルがある場合のみ横取りする。
        // 文字列のみの場合は既定の貼り付け動作(P-01への文字列貼り付け等)を妨げない。
        if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control) return;

        if (System.Windows.Clipboard.ContainsFileDropList())
        {
            var files = System.Windows.Clipboard.GetFileDropList().Cast<string>().ToList();
            _viewModel.PasteFiles(files);
            e.Handled = true;
            return;
        }

        if (!System.Windows.Clipboard.ContainsImage()) return;

        var image = System.Windows.Clipboard.GetImage();
        if (image != null)
        {
            _viewModel.PasteImage(image);
        }
        e.Handled = true;
    }

    // 04書 G-014: 投稿内容欄(P-01)にフォーカスがある場合のみ、Ctrl+Enterで投稿する。
    private void ContentTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!IsPostShortcut(e.Key, Keyboard.Modifiers)) return;

        e.Handled = true;
        if (_viewModel.PostCommand.CanExecute(null))
        {
            _viewModel.PostCommand.Execute(null);
        }
    }

    public static bool IsPostShortcut(Key key, ModifierKeys modifiers) =>
        key == Key.Enter && modifiers == ModifierKeys.Control;

    private async void LoadFromFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "テキストファイル (*.txt)|*.txt",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.LoadContentFromFileAsync(dialog.FileName);
        }
    }

    private void BrowseImageFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _viewModel.SetImagesFromFolder(dialog.SelectedPath);
        }
    }

    // 04書 G-011: サムネイルのドラッグ&ドロップ並べ替え。

    private void ThumbnailItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _thumbnailDragStartPoint = e.GetPosition(null);
    }

    private void ThumbnailItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsBusy || e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement { DataContext: AttachedImage image } element) return;

        var current = e.GetPosition(null);
        if (Math.Abs(current.X - _thumbnailDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _thumbnailDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        DragDrop.DoDragDrop(element, image, System.Windows.DragDropEffects.Move);
    }

    private void ThumbnailItem_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(AttachedImage))) return;
        if (e.Data.GetData(typeof(AttachedImage)) is not AttachedImage droppedImage) return;
        if (sender is not FrameworkElement { DataContext: AttachedImage targetImage }) return;
        if (ReferenceEquals(droppedImage, targetImage)) return;

        var fromIndex = _viewModel.AttachedImages.IndexOf(droppedImage);
        var toIndex = _viewModel.AttachedImages.IndexOf(targetImage);
        if (fromIndex < 0 || toIndex < 0) return;

        _viewModel.MoveImage(fromIndex, toIndex);
    }
}
