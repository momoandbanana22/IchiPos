using IchiPos.Content;
using Xunit;

namespace IchiPos.Tests.Content;

public class DatePlaceholderReplacerTests
{
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static DatePlaceholderReplacer CreateReplacer(int year, int month, int day)
    {
        var fixedNow = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        return new DatePlaceholderReplacer(new FixedTimeProvider(fixedNow));
    }

    [Fact]
    public void 正常系_プレースホルダなしはそのまま返す()
    {
        // Arrange
        var replacer = CreateReplacer(2026, 7, 3);

        // Act
        var result = replacer.Replace("hello");

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void 正常系_プレースホルダを実行日の日付に置換する()
    {
        // Arrange
        var replacer = CreateReplacer(2026, 7, 3);

        // Act
        var result = replacer.Replace("今日は{date}です");

        // Assert
        Assert.Equal("今日は2026/07/03です", result);
    }

    [Fact]
    public void 正常系_プレースホルダが複数ある場合すべて置換する()
    {
        // Arrange
        var replacer = CreateReplacer(2026, 1, 9);

        // Act
        var result = replacer.Replace("{date} から {date} まで");

        // Assert
        Assert.Equal("2026/01/09 から 2026/01/09 まで", result);
    }

    [Fact]
    public void 正常系_大文字小文字が異なるプレースホルダは置換しない()
    {
        // Arrange
        var replacer = CreateReplacer(2026, 7, 3);

        // Act
        var result = replacer.Replace("{Date} {DATE} {date}");

        // Assert
        Assert.Equal("{Date} {DATE} 2026/07/03", result);
    }

    [Fact]
    public void 正常系_空文字列はそのまま返す()
    {
        // Arrange
        var replacer = CreateReplacer(2026, 7, 3);

        // Act
        var result = replacer.Replace("");

        // Assert
        Assert.Equal("", result);
    }
}
