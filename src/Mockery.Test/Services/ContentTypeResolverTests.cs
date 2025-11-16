using FluentAssertions;
using Mockery.Services;
using Xunit;

namespace Mockery.Test.Services;

public class ContentTypeResolverTests
{
    private readonly IContentTypeResolver _resolver;

    public ContentTypeResolverTests()
    {
        _resolver = new ContentTypeResolver();
    }

    [Theory]
    [InlineData(".json", "application/json")]
    [InlineData(".html", "text/html")]
    [InlineData(".xml", "application/xml")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".pdf", "application/pdf")]
    public void GetContentType_WithKnownExtension_ReturnsCorrectContentType(string extension, string expectedContentType)
    {
        // Act
        var result = _resolver.GetContentType(extension);

        // Assert
        result.Should().Be(expectedContentType);
    }

    [Fact]
    public void GetContentType_WithUnknownExtension_ReturnsOctetStream()
    {
        // Act
        var result = _resolver.GetContentType(".unknown");

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void GetContentType_WithoutDot_AddsDotsAndReturnsContentType()
    {
        // Act
        var result = _resolver.GetContentType("json");

        // Assert
        result.Should().Be("application/json");
    }

    [Fact]
    public void GetContentType_WithNullOrEmpty_ReturnsOctetStream()
    {
        // Act
        var result1 = _resolver.GetContentType(null!);
        var result2 = _resolver.GetContentType("");

        // Assert
        result1.Should().Be("application/octet-stream");
        result2.Should().Be("application/octet-stream");
    }
}
