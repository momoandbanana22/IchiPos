using System.Globalization;

namespace IchiPos.Content;

public interface IDatePlaceholderReplacer
{
    string Replace(string content);
}

public class DatePlaceholderReplacer : IDatePlaceholderReplacer
{
    private const string Placeholder = "{date}";
    private const string DateFormat = "yyyy/MM/dd";

    private readonly TimeProvider _timeProvider;

    public DatePlaceholderReplacer(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string Replace(string content)
    {
        if (!content.Contains(Placeholder, StringComparison.Ordinal))
        {
            return content;
        }

        // 日付は投稿本文(プロセス境界)へ出るため、実行環境のカルチャ既定暦に依存させない。
        // InvariantCulture で確定的な yyyy/MM/dd(グレゴリオ暦)にする(issue #102)。
        var today = _timeProvider.GetLocalNow().ToString(DateFormat, CultureInfo.InvariantCulture);
        return content.Replace(Placeholder, today);
    }
}
