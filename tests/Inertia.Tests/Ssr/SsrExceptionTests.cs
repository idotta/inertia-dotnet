using Inertia.Core.Ssr;
using Xunit;

namespace Inertia.Tests.Ssr;

public class SsrExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesException()
    {
        // Act
        var exception = new SsrException();

        // Assert
        Assert.NotNull(exception);
        Assert.NotNull(exception.Message); // Exception always has a default message
        Assert.Null(exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "SSR failed";

        // Act
        var exception = new SsrException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "SSR failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SsrException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Null(exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithMessageAndSsrUrl_SetsProperties()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";

        // Act
        var exception = new SsrException(message, ssrUrl);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(ssrUrl, exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithMessageSsrUrlAndDiagnosticInfo_SetsAllProperties()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";
        var diagnosticInfo = "Server returned 500";

        // Act
        var exception = new SsrException(message, ssrUrl, diagnosticInfo);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(ssrUrl, exception.SsrUrl);
        Assert.Equal(diagnosticInfo, exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";
        var diagnosticInfo = "Server returned 500";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SsrException(message, ssrUrl, diagnosticInfo, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(ssrUrl, exception.SsrUrl);
        Assert.Equal(diagnosticInfo, exception.DiagnosticInfo);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullSsrUrl_AcceptsNull()
    {
        // Arrange
        var message = "SSR failed";

        // Act
        var exception = new SsrException(message, ssrUrl: null);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void Constructor_WithNullDiagnosticInfo_AcceptsNull()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";

        // Act
        var exception = new SsrException(message, ssrUrl, diagnosticInfo: null);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(ssrUrl, exception.SsrUrl);
        Assert.Null(exception.DiagnosticInfo);
    }

    [Fact]
    public void CanBeThrown_AsStandardException()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";

        // Act & Assert
        void ThrowException() => throw new SsrException(message, ssrUrl);
        var exception = Assert.Throws<SsrException>(ThrowException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(ssrUrl, exception.SsrUrl);
    }

    [Fact]
    public void CanBeCaught_AsException()
    {
        // Arrange
        var message = "SSR failed";
        var ssrUrl = "http://localhost:13714";

        // Act
        Exception? caughtException = null;
        try
        {
            throw new SsrException(message, ssrUrl);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.IsType<SsrException>(caughtException);
    }
}
