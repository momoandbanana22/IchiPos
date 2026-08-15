namespace IchiPos.CommandLine;

public interface ICommandLineParser
{
    ParseResult Parse(string[] args);
}

public class CommandLineParser : ICommandLineParser
{
    public ParseResult Parse(string[] args)
    {
        var contents = new List<string>();
        string? imagePath = null;
        // 添付画像のセンシティブフラグ（issue #107）。既定は ON。--sensitive off でのみ OFF になる。
        bool isSensitive = true;
        int index = 0;

        while (index < args.Length)
        {
            var arg = args[index];

            if (arg == "--version")
            {
                return ParseResult.VersionRequest();
            }
            else if (arg == "--image-path")
            {
                if (index + 1 >= args.Length)
                    return ParseResult.Failure("--image-path の後にフォルダパスが指定されていません");
                imagePath = args[index + 1];
                index += 2;
            }
            else if (arg == "--sensitive")
            {
                if (index + 1 >= args.Length)
                    return ParseResult.Failure("--sensitive の後に on / off が指定されていません");
                var value = args[index + 1];
                if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
                    isSensitive = true;
                else if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
                    isSensitive = false;
                else
                    return ParseResult.Failure($"--sensitive の値は on / off のいずれかである必要があります（指定値: {value}）");
                index += 2;
            }
            else if (arg.StartsWith("--"))
            {
                return ParseResult.Failure($"未定義のオプション: {arg}");
            }
            else
            {
                contents.Add(arg);
                index++;
            }
        }

        if (contents.Count == 0)
            return ParseResult.Failure("contentが指定されていません");
        if (contents.Count > 1)
            return ParseResult.Failure("contentが複数指定されています");

        return ParseResult.Success(contents[0], imagePath, isSensitive);
    }
}
