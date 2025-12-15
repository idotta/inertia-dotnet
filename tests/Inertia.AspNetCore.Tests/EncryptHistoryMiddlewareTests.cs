using Microsoft.AspNetCore.Http;
using Xunit;

namespace Inertia.AspNetCore.Tests;

public class EncryptHistoryMiddlewareTests
{
    // Test double for IInertia
    private class TestInertia : Core.IInertia
    {
        public int EncryptHistoryCalls { get; private set; }

        public Task<Core.InertiaResponse> RenderAsync(string component, IDictionary<string, object?>? props = null) =>
            throw new NotImplementedException();
        public object Location(string url) => throw new NotImplementedException();
        public void Share(string key, object? value) => throw new NotImplementedException();
        public void Share(IDictionary<string, object?> props) => throw new NotImplementedException();
        public object? GetShared(string? key = null, object? defaultValue = null) => throw new NotImplementedException();
        public void FlushShared() => throw new NotImplementedException();
        public void SetVersion(string version) => throw new NotImplementedException();
        public void SetVersion(Func<string> versionProvider) => throw new NotImplementedException();
        public string GetVersion() => throw new NotImplementedException();
        public void SetRootView(string viewName) => throw new NotImplementedException();
        public void ClearHistory() => throw new NotImplementedException();
        public void EncryptHistory(bool encrypt = true)
        {
            EncryptHistoryCalls++;
        }
        public void ResolveUrlUsing(Func<string>? urlResolver) => throw new NotImplementedException();
    }

    [Fact]
    public async Task InvokeAsync_CallsEncryptHistoryOnInertia()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();
        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(context, Next);

        // Assert
        Assert.Equal(1, testInertia.EncryptHistoryCalls);
        Assert.True(nextCalled, "Next middleware should have been called");
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();
        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled, "Next middleware should have been called");
    }

    [Fact]
    public async Task InvokeAsync_WorksWithMultipleRequests()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();
        var nextCallCount = 0;
        Task Next(HttpContext ctx)
        {
            nextCallCount++;
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(context1, Next);
        await middleware.InvokeAsync(context2, Next);

        // Assert
        Assert.Equal(2, testInertia.EncryptHistoryCalls);
        Assert.Equal(2, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_PropagatesExceptionsFromNext()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Test exception");
        Task Next(HttpContext ctx) => throw expectedException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, Next));

        Assert.Same(expectedException, exception);
        Assert.Equal(1, testInertia.EncryptHistoryCalls);
    }

    [Fact]
    public async Task InvokeAsync_PreservesHttpContextState()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();
        context.Items["TestKey"] = "TestValue";

        Task Next(HttpContext ctx)
        {
            Assert.Equal("TestValue", ctx.Items["TestKey"]);
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(context, Next);

        // Assert
        Assert.Equal("TestValue", context.Items["TestKey"]);
    }

    [Fact]
    public async Task InvokeAsync_CanBeUsedMultipleTimesInPipeline()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware1 = new EncryptHistoryMiddleware(testInertia);
        var middleware2 = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();
        var finalHandlerCalled = false;

        Task FinalHandler(HttpContext ctx)
        {
            finalHandlerCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await middleware1.InvokeAsync(context, ctx => middleware2.InvokeAsync(ctx, FinalHandler));

        // Assert
        Assert.Equal(2, testInertia.EncryptHistoryCalls);
        Assert.True(finalHandlerCalled);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotModifyResponse()
    {
        // Arrange
        var testInertia = new TestInertia();
        var middleware = new EncryptHistoryMiddleware(testInertia);
        var context = new DefaultHttpContext();

        Task Next(HttpContext ctx)
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(context, Next);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
    }
}
