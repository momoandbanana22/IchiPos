using IchiPos;
using IchiPos.CommandLine;
using IchiPos.Config;
using IchiPos.Content;
using IchiPos.Images;
using IchiPos.Output;
using IchiPos.Post;
using IchiPos.Validation;

namespace IchiPos.Application;

public interface IIchiPosApplication
{
    /// <summary>CLI入力受付（F-001）を経由する実行。コマンドライン引数を解析する。</summary>
    Task<int> RunAsync(string[] args, AppConfig config);

    /// <summary>
    /// GUI入力受付（04書 G-005）を経由する実行。.txtファイル判定（F-002）は行わない。画像は添付ファイルのフルパスのリストで受け取る。
    /// <paramref name="isSensitive"/> は添付画像のセンシティブフラグ（issue #107、04書 G-018）。
    /// Misskey・X に同じ本文を投稿する（通常の投稿）。
    /// </summary>
    Task<int> RunAsync(string content, IReadOnlyList<string> imagePaths, AppConfig config, bool isSensitive);

    /// <summary>
    /// GUI入力受付（04書 G-005）を経由する実行のうち、Misskey本文とX本文を分けて投稿する版（issue #111、04書 G-016）。
    /// Misskey には <paramref name="content"/> を、X投稿画面（下書き）には <paramref name="xContent"/> を用いる。
    /// 定型文で Misskey・X に別本文を投稿する場合に用いる（通常の投稿では上の4引数版を使い、X本文は投稿本文と同一になる）。
    /// </summary>
    Task<int> RunAsync(string content, IReadOnlyList<string> imagePaths, AppConfig config, bool isSensitive, string xContent);
}

public class IchiPosApplication : IIchiPosApplication
{
    /// <summary>Xが1投稿に添付できる画像の最大枚数。</summary>
    private const int XMaxImageAttachCount = 4;

    private readonly ICommandLineParser _commandLineParser;
    private readonly IContentResolver _contentResolver;
    private readonly IDatePlaceholderReplacer _datePlaceholderReplacer;
    private readonly IImageFolderReader _imageFolderReader;
    private readonly IImageValidator _imageValidator;
    private readonly IPrePostValidator _prePostValidator;
    private readonly IMisskeyPoster _misskeyPoster;
    private readonly IXPostLauncher _xPostLauncher;
    private readonly IOutputWriter _outputWriter;
    private readonly IClipboardService _clipboardService;
    private readonly IImageCleanupService _imageCleanupService;

    public IchiPosApplication(
        ICommandLineParser commandLineParser,
        IContentResolver contentResolver,
        IDatePlaceholderReplacer datePlaceholderReplacer,
        IImageFolderReader imageFolderReader,
        IImageValidator imageValidator,
        IPrePostValidator prePostValidator,
        IMisskeyPoster misskeyPoster,
        IXPostLauncher xPostLauncher,
        IOutputWriter outputWriter,
        IClipboardService clipboardService,
        IImageCleanupService imageCleanupService)
    {
        _commandLineParser = commandLineParser;
        _contentResolver = contentResolver;
        _datePlaceholderReplacer = datePlaceholderReplacer;
        _imageFolderReader = imageFolderReader;
        _imageValidator = imageValidator;
        _prePostValidator = prePostValidator;
        _misskeyPoster = misskeyPoster;
        _xPostLauncher = xPostLauncher;
        _outputWriter = outputWriter;
        _clipboardService = clipboardService;
        _imageCleanupService = imageCleanupService;
    }

    public async Task<int> RunAsync(string[] args, AppConfig config)
    {
        // 1. コマンドライン引数を解析
        var parseResult = _commandLineParser.Parse(args);
        if (parseResult.IsVersionRequest)
        {
            _outputWriter.WriteInfo($"IchiPos {AppVersion.Current}");
            return 0;
        }
        if (!parseResult.IsSuccess)
        {
            _outputWriter.WriteError($"入力エラー: {parseResult.ErrorMessage}");
            return 1;
        }

        // 2. 投稿テキストを取得（.txtファイル判定・日付埋め込みを含む。CLI固有の入力受付規則）
        var contentResult = await _contentResolver.ResolveAsync(parseResult.Content!);
        if (!contentResult.IsSuccess)
        {
            _outputWriter.WriteError($"投稿内容エラー: {contentResult.ErrorMessage}");
            return 1;
        }

        return await RunFolderPostPipelineAsync(contentResult.Content!, parseResult.ImagePath, config, parseResult.IsSensitive);
    }

    // 通常の投稿（Misskey・X 同一本文）。X本文を投稿本文と同一にして本体（5引数版）へ委譲する。
    public Task<int> RunAsync(string content, IReadOnlyList<string> imagePaths, AppConfig config, bool isSensitive)
        => RunAsync(content, imagePaths, config, isSensitive, content);

    public async Task<int> RunAsync(string content, IReadOnlyList<string> imagePaths, AppConfig config, bool isSensitive, string xContent)
    {
        // GUI入力: .txtファイル判定（F-002）は行わず、常に文字列として扱う。
        // 日付プレースホルダ置換（F-013）のみ、投稿実行時にCLIと同じ規則で適用する（Misskey本文・X本文の双方に）。
        var replacedMisskeyContent = _datePlaceholderReplacer.Replace(content);
        var replacedXContent = _datePlaceholderReplacer.Replace(xContent);
        return await RunListPostPipelineAsync(replacedMisskeyContent, replacedXContent, imagePaths, config, isSensitive);
    }

    /// <summary>画像一覧取得〜投稿前チェックまで（F-004〜F-005）。CLI専用: フォルダパスから画像一覧を解決する。</summary>
    private async Task<int> RunFolderPostPipelineAsync(string content, string? imagePath, AppConfig config, bool isSensitive)
    {
        _outputWriter.WriteInfo($"投稿テキストを取得しました（{content.Length}文字）");

        // 3. 画像一覧を取得
        var imageFolderResult = _imageFolderReader.Read(imagePath);
        if (!imageFolderResult.IsSuccess)
        {
            _outputWriter.WriteError($"画像フォルダエラー: {imageFolderResult.ErrorMessage}");
            return 1;
        }

        // 4. 画像添付対象判定
        var imageValidationResult = _imageValidator.Validate(
            imagePath ?? "",
            imageFolderResult.ImageFiles);
        if (!imageValidationResult.IsSuccess)
        {
            _outputWriter.WriteError($"画像エラー: {imageValidationResult.ErrorMessage}");
            return 1;
        }
        _outputWriter.WriteInfo($"添付画像: {imageValidationResult.ValidImagePaths.Count}枚");

        // CLIは Misskey・X に同じ本文を投稿する（xContent は content と同一。issue #111、G-016）。
        return await RunCommonPipelineAsync(content, content, imageValidationResult.ValidImagePaths, config, isSensitive);
    }

    /// <summary>画像添付対象判定〜投稿前チェックまで（F-005）。GUI専用: 画面が管理する画像ファイルのフルパスのリストをそのまま検証する。</summary>
    private async Task<int> RunListPostPipelineAsync(string misskeyContent, string xContent, IReadOnlyList<string> imagePaths, AppConfig config, bool isSensitive)
    {
        _outputWriter.WriteInfo($"投稿テキストを取得しました（{misskeyContent.Length}文字）");

        // 画像添付対象判定（フォルダ結合は行わず、渡されたフルパスをそのまま検証する）
        var imageValidationResult = _imageValidator.ValidateFiles(imagePaths.ToList());
        if (!imageValidationResult.IsSuccess)
        {
            _outputWriter.WriteError($"画像エラー: {imageValidationResult.ErrorMessage}");
            return 1;
        }
        _outputWriter.WriteInfo($"添付画像: {imageValidationResult.ValidImagePaths.Count}枚");

        return await RunCommonPipelineAsync(misskeyContent, xContent, imageValidationResult.ValidImagePaths, config, isSensitive);
    }

    /// <summary>
    /// 投稿前チェック〜画像削除まで（F-006〜F-011）。CLI/GUI共通の投稿パイプライン。
    /// Misskey には <paramref name="misskeyContent"/> を、X投稿画面には <paramref name="xContent"/> を用いる。
    /// 通常の投稿・CLI では両者は同一（issue #111、04書 G-016）。
    /// </summary>
    private async Task<int> RunCommonPipelineAsync(string misskeyContent, string xContent, List<string> validImagePaths, AppConfig config, bool isSensitive)
    {
        // 投稿テキストの自動トリミングは対象外（02書「v1対象外機能」節）だが、入力経路（直接入力・貼り付け・
        // ファイル読み込み）によらず投稿直前に文末の空白・改行だけを1回だけ除去する例外を設ける（F-006）。
        // Misskey本文・X本文の双方に適用する。
        misskeyContent = misskeyContent.TrimEnd();
        xContent = xContent.TrimEnd();

        // 5. 投稿前チェック（本文ごとに個別の上限で検証。issue #111、G-016第6節）。
        //    Misskey本文は misskey_max_length、X本文は x_max_length に対して検証する。
        //    両本文が同一の場合（通常の投稿・CLI）は、短い方の上限で一律に弾くのと結果は同じになる。
        //    画像の読み込み可否検証は Misskey本文側の1回のみ行う（同じ添付画像のため二重検証しない）。
        var misskeyValidation = _prePostValidator.Validate(
            misskeyContent,
            validImagePaths,
            config.Limits.MisskeyMaxLength);
        if (!misskeyValidation.IsSuccess)
        {
            _outputWriter.WriteError($"検証エラー: {misskeyValidation.ErrorMessage}");
            return 1;
        }

        var xValidation = _prePostValidator.Validate(
            xContent,
            new List<string>(),
            config.Limits.XMaxLength);
        if (!xValidation.IsSuccess)
        {
            _outputWriter.WriteError($"検証エラー: {xValidation.ErrorMessage}");
            return 1;
        }

        // 6. Misskeyに投稿（添付画像のセンシティブフラグを含む。issue #107）
        var misskeyResult = await _misskeyPoster.PostAsync(
            misskeyContent,
            validImagePaths,
            config,
            isSensitive);
        if (!misskeyResult.IsSuccess)
        {
            _outputWriter.WriteError($"Misskey投稿エラー: {misskeyResult.ErrorMessage}");
            return 1;
        }

        _outputWriter.WriteSuccess($"Misskey投稿成功: {misskeyResult.NoteId}");

        // 7. X投稿画面を起動（X本文を用いる）
        var xResult = await _xPostLauncher.LaunchAsync(xContent, config);
        if (!xResult.IsSuccess)
        {
            _outputWriter.WriteError($"X投稿準備エラー: {xResult.ErrorMessage}");
            // Misskey投稿は成功しているのでエラーにはしない
            return 0;
        }

        _outputWriter.WriteSuccess("X投稿画面起動成功");

        // X Intent URL では画像を渡せないため、ユーザーが Ctrl+V で貼り付けられるよう
        // 画像をファイルドロップリストとしてクリップボードにコピーする（X は最大4枚まで添付可能）。
        if (validImagePaths.Count > 0)
        {
            var copiedPaths = validImagePaths.Take(XMaxImageAttachCount).ToList();
            var total = validImagePaths.Count;
            var copiedCount = copiedPaths.Count;

            // コピーの成否を確認し、成功時のみ完了を通知する。失敗（クリップボード競合等）は
            // 握りつぶさずエラーとして出す(issue #56)。Misskey投稿は成功済みのため終了コードは0のまま。
            try
            {
                _clipboardService.SetImages(copiedPaths);
                if (total <= copiedCount)
                    _outputWriter.WriteInfo($"画像をクリップボードにコピーしました（全{total}枚）。X下書き画面で Ctrl+V で貼り付けてください。");
                else
                    _outputWriter.WriteInfo($"先頭{copiedCount}枚の画像をクリップボードにコピーしました（全{total}枚）。X下書き画面で Ctrl+V で貼り付けてください。残り{total - copiedCount}枚は手動で添付してください。");
            }
            catch (ClipboardCopyException ex)
            {
                _outputWriter.WriteError($"画像のクリップボードへのコピーに失敗しました: {ex.Message} 画像はX下書き画面へ手動で添付してください。");
            }

            await _imageCleanupService.RunAsync(validImagePaths);
        }

        return 0;
    }
}
