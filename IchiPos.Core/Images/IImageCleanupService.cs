namespace IchiPos.Images;

public interface IImageCleanupService
{
    Task RunAsync(List<string> imagePaths);
}
