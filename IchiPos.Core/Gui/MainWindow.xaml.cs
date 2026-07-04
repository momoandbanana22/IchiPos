using System.Windows;
using System.Windows.Input;

namespace IchiPos.Gui;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 04書 G-010: クリップボードに画像がある場合のみ横取りする。
        // 文字列のみの場合は既定の貼り付け動作(P-01への文字列貼り付け等)を妨げない。
        if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control) return;
        if (!System.Windows.Clipboard.ContainsImage()) return;

        var image = System.Windows.Clipboard.GetImage();
        if (image != null)
        {
            _viewModel.PasteImage(image);
        }
        e.Handled = true;
    }

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
            _viewModel.ImageFolderPath = dialog.SelectedPath;
        }
    }
}
