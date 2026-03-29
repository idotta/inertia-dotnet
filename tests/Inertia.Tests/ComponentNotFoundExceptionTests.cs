using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class ComponentNotFoundExceptionTests
{
    [Fact]
    public void InheritsFromInvalidOperationException()
    {
        var exception = new ComponentNotFoundException("test");

        exception.Should().BeAssignableTo<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        const string message = "Component 'Users/Index' not found.";

        var exception = new ComponentNotFoundException(message);

        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        const string message = "Component 'Users/Index' not found.";
        var inner = new InvalidOperationException("inner");

        var exception = new ComponentNotFoundException(message, inner);

        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(inner);
    }
}
