namespace IchiPos.Output;

public interface IClipboardService
{
    void SetImages(IReadOnlyList<string> imagePaths);
}
