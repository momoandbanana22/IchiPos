using IchiPos.Output;

namespace IchiPos.Images;

public class ImageCleanupService : IImageCleanupService
{
    private readonly IUserPrompt _userPrompt;
    private readonly IOutputWriter _outputWriter;
    private readonly Action<string> _deleteFile;

    public ImageCleanupService(IUserPrompt userPrompt, IOutputWriter outputWriter)
        : this(userPrompt, outputWriter, File.Delete) { }

    public ImageCleanupService(IUserPrompt userPrompt, IOutputWriter outputWriter, Action<string> deleteFile)
    {
        _userPrompt = userPrompt;
        _outputWriter = outputWriter;
        _deleteFile = deleteFile;
    }

    public Task RunAsync(List<string> imagePaths)
    {
        if (imagePaths.Count == 0) return Task.CompletedTask;

        var answer = _userPrompt.Ask($"画像を削除してよいですか？（{imagePaths.Count}枚）(y/n): ");
        if (answer?.Trim().ToLowerInvariant() is "y" or "yes")
        {
            foreach (var path in imagePaths)
                _deleteFile(path);
            _outputWriter.WriteInfo($"画像を{imagePaths.Count}枚削除しました。");
        }
        else
        {
            _outputWriter.WriteInfo("画像の削除をスキップしました。");
        }

        return Task.CompletedTask;
    }
}
