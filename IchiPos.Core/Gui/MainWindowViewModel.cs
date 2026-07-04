using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Output;

namespace IchiPos.Gui;

/// <summary>GUIメイン画面のViewModel(04書 第5〜6節の画面項目・機能に対応)。</summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IIchiPosApplication _app;
    private readonly AppConfig _config;
    private readonly ITextFileReader _textFileReader;
    private readonly GuiOutputWriter _outputWriter;
    private readonly IClipboardImageStore _clipboardImageStore;
    private readonly AsyncRelayCommand _postCommand;

    private string _content = string.Empty;
    private string? _imageFolderPath;
    private string? _pastedImageTempFolder;
    private bool _deleteImagesAfterPost;
    private bool _isBusy;

    public MainWindowViewModel(
        IIchiPosApplication app,
        AppConfig config,
        ITextFileReader textFileReader,
        GuiOutputWriter outputWriter,
        IClipboardImageStore clipboardImageStore)
    {
        _app = app;
        _config = config;
        _textFileReader = textFileReader;
        _outputWriter = outputWriter;
        _clipboardImageStore = clipboardImageStore;

        _postCommand = new AsyncRelayCommand(PostAsync, () => !IsBusy);
        ClearImageFolderCommand = new RelayCommand(() => ImageFolderPath = null);
        ClearLogCommand = new RelayCommand(() => _outputWriter.Clear());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>P-01: 投稿内容。</summary>
    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                OnPropertyChanged(nameof(CharacterCountDisplay));
            }
        }
    }

    /// <summary>
    /// P-02: 文字数表示。{date}未置換の文字数をそのまま表示する目安表示であり、
    /// 実際の上限チェックは投稿実行時に置換後のテキストに対して行う(04書 G-002 第5節)。
    /// </summary>
    public string CharacterCountDisplay => $"{Content.Length} / {MaxLength} 文字";

    private int MaxLength => Math.Min(_config.Limits.MisskeyMaxLength, _config.Limits.XMaxLength);

    /// <summary>P-04: 画像フォルダパス。未選択の場合はnull。</summary>
    public string? ImageFolderPath
    {
        get => _imageFolderPath;
        set
        {
            var previousPastedFolder = _pastedImageTempFolder;

            if (SetProperty(ref _imageFolderPath, value))
            {
                OnPropertyChanged(nameof(IsDeleteCheckboxEnabled));
            }

            // 04書 G-003 第3.1節: フォルダ選択・クリア・貼り付けは互いに排他。
            // 貼り付けで作成した一時フォルダが今回の変更で不要になった場合は削除する。
            if (previousPastedFolder != null && previousPastedFolder != value)
            {
                _clipboardImageStore.Delete(previousPastedFolder);
                _pastedImageTempFolder = null;
            }
        }
    }

    /// <summary>P-07: 投稿後に画像を削除するかどうかの事前設定(04書 G-004)。</summary>
    public bool DeleteImagesAfterPost
    {
        get => _deleteImagesAfterPost;
        set => SetProperty(ref _deleteImagesAfterPost, value);
    }

    /// <summary>画像フォルダ未設定時はP-07を無効化する(04書 G-004 第4節)。</summary>
    public bool IsDeleteCheckboxEnabled => !string.IsNullOrWhiteSpace(ImageFolderPath);

    /// <summary>投稿処理実行中かどうか(04書 G-007 二重投稿防止)。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                _postCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Viewの入力コントロールのIsEnabledバインディング用(!IsBusy)。</summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>P-11: バージョン表示(04書 G-008)。</summary>
    public string VersionText => $"IchiPos {AppVersion.Current}";

    /// <summary>P-09: 結果ログ(04書 G-006)。</summary>
    public ObservableCollection<LogEntry> LogEntries => _outputWriter.Entries;

    /// <summary>P-08: 投稿するボタン。</summary>
    public ICommand PostCommand => _postCommand;

    /// <summary>P-06: 画像フォルダの選択解除(04書 G-003 第2節)。</summary>
    public ICommand ClearImageFolderCommand { get; }

    /// <summary>P-10: ログをクリア(04書 G-006 第5節)。</summary>
    public ICommand ClearLogCommand { get; }

    /// <summary>投稿実行(04書 G-005)。画像一覧取得〜画像削除までApplication層の共通パイプラインを呼ぶ。</summary>
    public async Task PostAsync()
    {
        IsBusy = true;
        try
        {
            var imagePath = string.IsNullOrWhiteSpace(ImageFolderPath) ? null : ImageFolderPath;
            await _app.RunAsync(Content, imagePath, _config);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// クリップボード画像の貼り付け(04書 G-010)。画像を一時フォルダへ保存し、画像フォルダパスに設定する。
    /// 投稿処理実行中(IsBusy)は無視する。
    /// </summary>
    public void PasteImage(BitmapSource image)
    {
        if (IsBusy) return;

        var folder = _clipboardImageStore.SaveToTempFolder(image);
        ImageFolderPath = folder;
        _pastedImageTempFolder = folder;
    }

    /// <summary>P-03: ファイルから読み込む(04書 G-002 第4節)。読み込み成功時のみ投稿内容を置き換える。</summary>
    public async Task LoadContentFromFileAsync(string filePath)
    {
        var result = await _textFileReader.ReadAsync(filePath);
        if (result.IsSuccess)
        {
            Content = result.Content!;
        }
        else
        {
            _outputWriter.WriteError($"投稿内容エラー: {result.ErrorMessage}");
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
