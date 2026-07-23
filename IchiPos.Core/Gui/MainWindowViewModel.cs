using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using IchiPos.Application;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;

namespace IchiPos.Gui;

/// <summary>GUIメイン画面のViewModel(04書「画面構成」節・「機能一覧」節の画面項目・機能に対応)。</summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IIchiPosApplication _app;
    private readonly AppConfig _config;
    private readonly ITextFileReader _textFileReader;
    private readonly GuiOutputWriter _outputWriter;
    private readonly IClipboardImageStore _clipboardImageStore;
    private readonly IImageFolderReader _imageFolderReader;
    private readonly IDatePlaceholderReplacer _datePlaceholderReplacer;
    private readonly ILastPostStore _lastPostStore;
    private readonly IRepostConfirmation _repostConfirmation;
    private readonly AsyncRelayCommand _postCommand;
    private readonly AsyncRelayCommand<string> _postTemplateCommand;

    private string _content = string.Empty;
    private bool _isBusy;

    public MainWindowViewModel(
        IIchiPosApplication app,
        AppConfig config,
        ITextFileReader textFileReader,
        GuiOutputWriter outputWriter,
        IClipboardImageStore clipboardImageStore,
        IImageFolderReader imageFolderReader,
        IDatePlaceholderReplacer datePlaceholderReplacer,
        ILastPostStore lastPostStore,
        IRepostConfirmation repostConfirmation)
    {
        _app = app;
        _config = config;
        _textFileReader = textFileReader;
        _outputWriter = outputWriter;
        _clipboardImageStore = clipboardImageStore;
        _imageFolderReader = imageFolderReader;
        _datePlaceholderReplacer = datePlaceholderReplacer;
        _lastPostStore = lastPostStore;
        _repostConfirmation = repostConfirmation;

        _postCommand = new AsyncRelayCommand(PostAsync, () => !IsBusy);
        _postTemplateCommand = new AsyncRelayCommand<string>(text => PostTemplateAsync(text ?? string.Empty), _ => !IsBusy);
        RemoveImageCommand = new RelayCommand<AttachedImage>(RemoveImage);
        ClearImagesCommand = new RelayCommand(ClearImages);
        ClearContentCommand = new RelayCommand(() => Content = string.Empty);
        ClearLogCommand = new RelayCommand(() => _outputWriter.Clear());
        ClearAllCommand = new RelayCommand(ClearAll);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>全クリア(04書 G-013第3節)実行後、投稿内容欄(P-01)へフォーカスを移すようViewへ要求する。</summary>
    public event EventHandler? FocusContentRequested;

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

    /// <summary>P-04〜: 添付画像候補の一覧(サムネイル一覧、04書 G-013)。追加順を保持する。</summary>
    public ObservableCollection<AttachedImage> AttachedImages { get; } = new();

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
                _postTemplateCommand.RaiseCanExecuteChanged();
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

    /// <summary>P-17: 定型文一覧(04書 G-016 第3節)。設定の登録順をそのまま表示順とする。</summary>
    public IReadOnlyList<string> Templates => _config.Templates;

    /// <summary>
    /// 定型文が1件以上登録されているか(04書 G-016 第3節第4項)。
    /// falseの場合、Viewは一覧の代わりに未登録の案内を表示する。
    /// </summary>
    public bool HasTemplates => Templates.Count > 0;

    /// <summary>P-18: 定型文の投稿ボタン(04書 G-016)。パラメータにその行の定型文テキストを受け取る。</summary>
    public ICommand PostTemplateCommand => _postTemplateCommand;

    /// <summary>サムネイル一覧の個別削除ボタン(04書 G-013)。</summary>
    public ICommand RemoveImageCommand { get; }

    /// <summary>P-06: 添付画像一覧の全解除(04書 G-003 第2節)。</summary>
    public ICommand ClearImagesCommand { get; }

    /// <summary>P-14: 投稿内容欄の全消去(04書 G-012)。</summary>
    public ICommand ClearContentCommand { get; }

    /// <summary>P-15: 投稿内容・添付画像・結果ログの一括消去(04書 G-013)。</summary>
    public ICommand ClearAllCommand { get; }

    /// <summary>P-10: ログをクリア(04書 G-006 第5節)。</summary>
    public ICommand ClearLogCommand { get; }

    /// <summary>
    /// 投稿実行(04書 G-005)。投稿内容欄(P-01)と添付画像一覧(P-13)の現在の内容を投稿する。
    /// </summary>
    public Task PostAsync()
        => ExecutePostAsync(Content, AttachedImages.Select(i => i.FilePath).ToList());

    /// <summary>
    /// 定型文の投稿実行(04書 G-016)。投稿内容欄(P-01)・添付画像一覧(P-13)は参照も変更もせず、
    /// 渡された定型文テキストのみを画像添付なしで投稿する(G-016第4.1節)。
    /// 投稿経路が違うだけで、二重投稿防止(G-007)・再投稿確認(G-015)・投稿処理層は通常の投稿と共通とする。
    /// </summary>
    public Task PostTemplateAsync(string templateText)
        => ExecutePostAsync(templateText, Array.Empty<string>());

    /// <summary>
    /// 通常の投稿(G-005)と定型文投稿(G-016)で共通の投稿実行。Application層の共通パイプラインを呼ぶ。
    /// パイプラインに入る前に、前回投稿内容と同一なら再投稿の確認を行う(G-015)。
    /// </summary>
    private async Task ExecutePostAsync(string content, IReadOnlyList<string> imagePaths)
    {
        IsBusy = true;
        try
        {
            // 実際に投稿されるテキストと同じ形(日付置換済み・末尾トリム済み)に揃えて比較する(G-015第2節)。
            var contentHash = PostContentHash.Compute(_datePlaceholderReplacer.Replace(content).TrimEnd());
            if (_lastPostStore.LoadHash() == contentHash && !_repostConfirmation.ConfirmRepost())
            {
                _outputWriter.WriteInfo("投稿を中止しました");
                return;
            }

            var exitCode = await _app.RunAsync(content, imagePaths, _config);
            if (exitCode == 0)
            {
                _lastPostStore.SaveHash(contentHash);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// P-05: 画像フォルダの選択(04書 G-003)。選択したフォルダ内の対応画像ファイルへ、
    /// 現在の添付画像一覧を全てクリアしたうえで置き換える(一時ファイルはクリア時に削除する)。
    /// フォルダ読み込みに失敗した場合は一覧を変更せずエラーをログに出す。
    /// </summary>
    public void SetImagesFromFolder(string folderPath)
    {
        var result = _imageFolderReader.Read(folderPath);
        if (!result.IsSuccess)
        {
            _outputWriter.WriteError($"画像フォルダエラー: {result.ErrorMessage}");
            return;
        }

        ClearImages();
        foreach (var fileName in result.ImageFiles)
        {
            AttachedImages.Add(new AttachedImage(Path.Combine(folderPath, fileName), isTemporary: false));
        }
    }

    /// <summary>
    /// クリップボード画像の貼り付け(04書 G-010)。画像を一時ファイルへ保存し、添付画像一覧へ追加する
    /// (既存の一覧は置き換えず、末尾に追加する)。投稿処理実行中(IsBusy)は無視する。
    /// </summary>
    public void PasteImage(BitmapSource image)
    {
        if (IsBusy) return;

        var filePath = _clipboardImageStore.SaveToTempFile(image);
        AttachedImages.Add(new AttachedImage(filePath, isTemporary: true));
    }

    /// <summary>
    /// クリップボードの複数ファイル(エクスプローラーでの複数選択コピー等)の貼り付け(04書 G-010、issue #13)。
    /// 対応画像形式のファイルの実パスを添付画像一覧へ追加する(コピーしない)。
    /// 非対応拡張子のファイルは除外し、除外したファイル名を結果ログに警告として表示する。
    /// 投稿処理実行中(IsBusy)は無視する。
    /// </summary>
    public void PasteFiles(IReadOnlyList<string> filePaths)
    {
        if (IsBusy) return;

        var skipped = new List<string>();
        foreach (var filePath in filePaths)
        {
            if (SupportedImageExtensions.IsSupported(filePath))
            {
                AttachedImages.Add(new AttachedImage(filePath, isTemporary: false));
            }
            else
            {
                skipped.Add(Path.GetFileName(filePath));
            }
        }

        if (skipped.Count > 0)
        {
            _outputWriter.WriteWarning($"非対応の画像形式のため除外しました: {string.Join(", ", skipped)}");
        }
    }

    /// <summary>
    /// サムネイル一覧のドラッグ&ドロップ並べ替え(04書 G-011)。範囲外のインデックスは無視する。
    /// 投稿処理実行中(IsBusy)は無視する。
    /// </summary>
    public void MoveImage(int fromIndex, int toIndex)
    {
        if (IsBusy) return;
        if (fromIndex < 0 || fromIndex >= AttachedImages.Count) return;
        if (toIndex < 0 || toIndex >= AttachedImages.Count) return;
        if (fromIndex == toIndex) return;

        AttachedImages.Move(fromIndex, toIndex);
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

    private void RemoveImage(AttachedImage? image)
    {
        if (image == null) return;

        AttachedImages.Remove(image);
        if (image.IsTemporary)
        {
            _clipboardImageStore.Delete(image.FilePath);
        }
    }

    private void ClearAll()
    {
        Content = string.Empty;
        ClearImages();
        _outputWriter.Clear();
        FocusContentRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ClearImages()
    {
        foreach (var image in AttachedImages)
        {
            if (image.IsTemporary)
            {
                _clipboardImageStore.Delete(image.FilePath);
            }
        }
        AttachedImages.Clear();
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
