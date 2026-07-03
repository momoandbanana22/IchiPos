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
    Task<int> RunAsync(string[] args, AppConfig config);
}

public class IchiPosApplication : IIchiPosApplication
{
    private readonly ICommandLineParser _commandLineParser;
    private readonly IContentResolver _contentResolver;
    private readonly IImageFolderReader _imageFolderReader;
    private readonly IImageValidator _imageValidator;
    private readonly IPrePostValidator _prePostValidator;
    private readonly IMisskeyPoster _misskeyPoster;
    private readonly IPostDestinationRunner _postDestinationRunner;
    private readonly IOutputWriter _outputWriter;
    private readonly IClipboardService _clipboardService;
    private readonly IImageCleanupService _imageCleanupService;

    public IchiPosApplication(
        ICommandLineParser commandLineParser,
        IContentResolver contentResolver,
        IImageFolderReader imageFolderReader,
        IImageValidator imageValidator,
        IPrePostValidator prePostValidator,
        IMisskeyPoster misskeyPoster,
        IPostDestinationRunner postDestinationRunner,
        IOutputWriter outputWriter,
        IClipboardService clipboardService,
        IImageCleanupService imageCleanupService)
    {
        _commandLineParser = commandLineParser;
        _contentResolver = contentResolver;
        _imageFolderReader = imageFolderReader;
        _imageValidator = imageValidator;
        _prePostValidator = prePostValidator;
        _misskeyPoster = misskeyPoster;
        _postDestinationRunner = postDestinationRunner;
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

        // 2. 投稿テキストを取得
        var contentResult = await _contentResolver.ResolveAsync(parseResult.Content!);
        if (!contentResult.IsSuccess)
        {
            _outputWriter.WriteError($"投稿内容エラー: {contentResult.ErrorMessage}");
            return 1;
        }
        _outputWriter.WriteInfo($"投稿テキストを取得しました（{contentResult.Content!.Length}文字）");

        // 3. 画像一覧を取得
        var imageFolderResult = _imageFolderReader.Read(parseResult.ImagePath);
        if (!imageFolderResult.IsSuccess)
        {
            _outputWriter.WriteError($"画像フォルダエラー: {imageFolderResult.ErrorMessage}");
            return 1;
        }

        // 4. 画像添付対象判定
        var imageValidationResult = _imageValidator.Validate(
            parseResult.ImagePath ?? "",
            imageFolderResult.ImageFiles);
        if (!imageValidationResult.IsSuccess)
        {
            _outputWriter.WriteError($"画像エラー: {imageValidationResult.ErrorMessage}");
            return 1;
        }
        _outputWriter.WriteInfo($"添付画像: {imageValidationResult.ValidImagePaths.Count}枚");

        // 5. 投稿前チェック（有効化されている投稿先のうち、より短い上限を実効上限とする）
        var lengthLimits = new List<int> { config.Limits.MisskeyMaxLength };
        if (config.Mixi2.Enabled) lengthLimits.Add(config.Limits.Mixi2MaxLength);
        if (config.X.Enabled) lengthLimits.Add(config.Limits.XMaxLength);
        var maxLength = lengthLimits.Min();

        var validationResult = _prePostValidator.Validate(
            contentResult.Content!,
            imageValidationResult.ValidImagePaths,
            maxLength);
        if (!validationResult.IsSuccess)
        {
            _outputWriter.WriteError($"検証エラー: {validationResult.ErrorMessage}");
            return 1;
        }

        // 6. Misskeyに投稿
        var misskeyResult = await _misskeyPoster.PostAsync(
            contentResult.Content!,
            imageValidationResult.ValidImagePaths,
            config);
        if (!misskeyResult.IsSuccess)
        {
            _outputWriter.WriteError($"Misskey投稿エラー: {misskeyResult.ErrorMessage}");
            return 1;
        }

        _outputWriter.WriteSuccess($"Misskey投稿成功: {misskeyResult.NoteId}");

        // 7. サブ投稿先（MIXI2 → X）を実行する。互いに独立して実行し、
        // 1つのサブ投稿先が失敗・スキップしても、他方の実行を妨げない。
        var subResult = await _postDestinationRunner.RunAsync(
            contentResult.Content!,
            imageValidationResult.ValidImagePaths,
            config);

        if (subResult.Mixi2.IsSuccess)
        {
            _outputWriter.WriteSuccess($"MIXI2投稿成功: {subResult.Mixi2.PostId}");
        }
        else if (subResult.Mixi2.IsSkipped)
        {
            _outputWriter.WriteInfo("MIXI2投稿はスキップされました（無効化されています）");
        }
        else
        {
            _outputWriter.WriteError($"MIXI2投稿エラー: {subResult.Mixi2.ErrorMessage}");
        }

        if (subResult.X.IsSuccess)
        {
            _outputWriter.WriteSuccess("X投稿画面起動成功");

            // X Intent URL では画像を渡せないため、ユーザーが Ctrl+V で貼り付けられるよう
            // 1枚目の画像をクリップボードにコピーする。
            if (imageValidationResult.ValidImagePaths.Count > 0)
            {
                _clipboardService.SetImage(imageValidationResult.ValidImagePaths[0]);
                var total = imageValidationResult.ValidImagePaths.Count;
                if (total == 1)
                    _outputWriter.WriteInfo("画像をクリップボードにコピーしました。X下書き画面で Ctrl+V で貼り付けてください。");
                else
                    _outputWriter.WriteInfo($"1枚目の画像をクリップボードにコピーしました（全{total}枚）。X下書き画面で Ctrl+V で貼り付けてください。残り{total - 1}枚は手動で添付してください。");
            }
        }
        else if (subResult.X.IsSkipped)
        {
            _outputWriter.WriteInfo("X投稿画面の起動はスキップされました（無効化されています）");
        }
        else
        {
            _outputWriter.WriteError($"X投稿準備エラー: {subResult.X.ErrorMessage}");
            // Misskey投稿は成功しているのでエラーにはしない
        }

        // 8. 画像削除（Misskey投稿成功後、サブ投稿先すべての処理が完了した時点で確認する。
        // サブ投稿先の成功・失敗・スキップは問わない）
        if (imageValidationResult.ValidImagePaths.Count > 0)
        {
            await _imageCleanupService.RunAsync(imageValidationResult.ValidImagePaths);
        }

        return 0;
    }
}
