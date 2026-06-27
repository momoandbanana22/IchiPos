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
    public void 正常系_添付画像が読み込み可能な場合は成功する()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string> { "/path/to/image1.png", "/path/to/image2.jpg" };
        var validator = new PrePostValidator(isImageReadable: _ => true);

        // Act
        var result = validator.Validate(content, imagePaths, MaxLength);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void 異常系_添付画像が読み込み不可能な場合はエラーを返す()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string> { "/path/to/broken.png" };
        var validator = new PrePostValidator(isImageReadable: _ => false);

        // Act
        var result = validator.Validate(content, imagePaths, MaxLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 異常系_複数画像のうち1枚が読み込み不可能な場合はエラーを返す()
    {
        // Arrange
        var content = "テスト投稿";
        var validPath = "/path/valid.png";
        var corruptPath = "/path/corrupt.png";
        var imagePaths = new List<string> { validPath, corruptPath };
        var validator = new PrePostValidator(
            isImageReadable: path => path == validPath);

        // Act
        var result = validator.Validate(content, imagePaths, MaxLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 正常系_添付画像なしの場合は画像チェックをスキップする()
    {
        // Arrange
        var content = "テスト投稿";
        var imagePaths = new List<string>();
        var validator = new PrePostValidator(isImageReadable: _ => false);

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
