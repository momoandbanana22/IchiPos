using IchiPos.Validation;
using Xunit;

namespace IchiPos.Tests.Validation;

public class PrePostValidatorTests
{
    private const int MaxLength = 280;

    [Fact]
    public void 正常系_有効な投稿テキスト()
    {
        // Arrange
        var content = "テスト投稿";
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, new List<string>(), MaxLength);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void 正常系_長さ制限ギリギリ()
    {
        // Arrange
        var content = new string('あ', MaxLength);
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, new List<string>(), MaxLength);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void 異常系_投稿テキストが空()
    {
        // Arrange
        var content = "";
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, new List<string>(), MaxLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_投稿テキストが長さ制限超過()
    {
        // Arrange
        var content = new string('あ', MaxLength + 1);
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, new List<string>(), MaxLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 正常系_画像あり()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string> { "image1.png", "image2.jpg" };
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, imagePaths, MaxLength);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void 正常系_画像なし()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string>();
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content, imagePaths, MaxLength);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void 異常系_投稿テキストがnull()
    {
        // Arrange
        string? content = null;
        var validator = new PrePostValidator();

        // Act
        var result = validator.Validate(content!, new List<string>(), MaxLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
