namespace IchiPos.CommandLine;

public interface ICommandLineParser
{
    ParseResult Parse(string[] args);
}

public class CommandLineParser : ICommandLineParser
{
    public ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return ParseResult.Failure("contentが指定されていません");
        }

        var content = args[0];
        string? imagePath = null;
        int index = 1;

        while (index < args.Length)
        {
            var arg = args[index];

            if (arg == "--image-path")
            {
                if (index + 1 >= args.Length)
                {
                    return ParseResult.Failure("--image-path の後にフォルダパスが指定されていません");
                }
                imagePath = args[index + 1];
                index += 2;
            }
            else
            {
                return ParseResult.Failure($"未定義のオプション: {arg}");
            }
        }

        return ParseResult.Success(content, imagePath);
    }
}
