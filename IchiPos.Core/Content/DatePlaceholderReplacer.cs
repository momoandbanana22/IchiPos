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

        var today = _timeProvider.GetLocalNow().ToString(DateFormat);
        return content.Replace(Placeholder, today);
    }
}
